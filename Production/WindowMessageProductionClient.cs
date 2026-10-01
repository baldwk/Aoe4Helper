using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aoe4Helper.Production
{
   /// <summary>通过游戏原有热键提交操作，适用于正常启动的客户端。</summary>
   public sealed class WindowMessageProductionClient
   {
      private const int BatchWindowMilliseconds = 40;
      private readonly object queueLock = new();
      private readonly List<ProductionRequest> pending = new();
      private bool processing;

      public Task<string> ProduceAsync(string building, int slot, int count, CancellationToken token,
         bool activateGame = false)
      {
         if (token.IsCancellationRequested) return Task.FromCanceled<string>(token);
         if (count <= 0) return Task.FromResult($"{building}：已发送 0/0 次生产按键。");
         var request = new ProductionRequest(building, ProductionMapping.SlotKey(slot), count, GetSelectionKey(building),
            activateGame, token);
         lock (queueLock)
         {
            pending.Add(request);
            if (!processing)
            {
               processing = true;
               _ = Task.Run(ProcessQueueAsync);
            }
         }
         return request.Completion.Task;
      }

      private async Task ProcessQueueAsync()
      {
         while (true)
         {
            // 让同一轮 DispatcherTimer/开关事件产生的请求进入同一批，不等待用户空闲。
            await Task.Delay(BatchWindowMilliseconds).ConfigureAwait(false);
            List<ProductionRequest> batch;
            lock (queueLock)
            {
               batch = new List<ProductionRequest>(pending);
               pending.Clear();
            }

            try
            {
               await SendBatchAsync(batch).ConfigureAwait(false);
               foreach (var request in batch)
               {
                  if (request.Token.IsCancellationRequested) request.Completion.TrySetCanceled(request.Token);
                  else request.Completion.TrySetResult($"{request.Building}：已发送 {request.Sent}/{request.Count} 次生产按键。");
               }
            }
            catch (Exception ex)
            {
               foreach (var request in batch)
               {
                  if (request.Token.IsCancellationRequested) request.Completion.TrySetCanceled(request.Token);
                  else request.Completion.TrySetException(new InvalidOperationException(
                     $"{request.Building}：已发送 {request.Sent}/{request.Count}；{ex.Message}", ex));
               }
            }
            finally
            {
               foreach (var request in batch) request.Cancellation.Dispose();
            }

            lock (queueLock)
            {
               if (pending.Count == 0)
               {
                  processing = false;
                  return;
               }
            }
         }
      }

      private static async Task SendBatchAsync(List<ProductionRequest> batch)
      {
         var active = batch.Where(request => !request.Token.IsCancellationRequested).ToList();
         if (active.Count == 0) return;
         IntPtr window = FindGameWindow();

         // 仅手动开启生产的请求允许切回游戏；定时请求不激活窗口。
         var focusRequest = active.FirstOrDefault(request => request.ActivateGame);
         if (focusRequest != null && !focusRequest.Token.IsCancellationRequested)
         {
            SetForegroundWindow(window);
            await Task.Delay(30).ConfigureAwait(false);
         }

         ProductionRequest lastInput = null;
         foreach (var group in active.GroupBy(request => request.Building))
         {
            bool selected = false;
            while (!selected)
            {
               var owner = group.FirstOrDefault(request => !request.Token.IsCancellationRequested);
               if (owner == null) break;
               try
               {
                  await PressAsync(window, owner.SelectionKey, owner.Building == "TownCenter", owner.Token).ConfigureAwait(false);
                  await Task.Delay(30, owner.Token).ConfigureAwait(false);
                  selected = true;
               }
               catch (OperationCanceledException) when (owner.Token.IsCancellationRequested) { }
            }
            if (!selected) continue;

            foreach (var request in group)
            {
               try
               {
                  for (int i = 0; i < request.Count; i++)
                  {
                     await PressAsync(window, request.ProductionKey, true, request.Token).ConfigureAwait(false);
                     request.Sent++;
                     lastInput = request;
                  }
               }
               catch (OperationCanceledException) when (request.Token.IsCancellationRequested) { }
            }
         }

         // 一批中的所有建筑完成后固定发送一次 Esc 取消选中。
         var last = lastInput != null && !lastInput.Token.IsCancellationRequested ? lastInput : null;
         if (last != null)
         {
            try { await PressAsync(window, 0x1B, true, last.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (last.Token.IsCancellationRequested) { }
         }
      }

      private static int GetSelectionKey(string building) => building switch
      {
         "TownCenter" => 0x20,
         "Archery" => 0x4B,
         "Barracks" => 0x4A,
         "Stable" => 0x4C,
         _ => throw new ArgumentException("未知建筑：" + building)
      };

      private sealed class ProductionRequest
      {
         public string Building { get; }
         public int ProductionKey { get; }
         public int Count { get; }
         public int SelectionKey { get; }
         public bool ActivateGame { get; }
         public CancellationToken Token { get; }
         public CancellationTokenRegistration Cancellation { get; }
         public int Sent { get; set; }
         public TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

         public ProductionRequest(string building, int productionKey, int count, int selectionKey,
            bool activateGame, CancellationToken token)
         {
            Building = building;
            ProductionKey = productionKey;
            Count = count;
            SelectionKey = selectionKey;
            ActivateGame = activateGame;
            Token = token;
            Cancellation = token.Register(() => Completion.TrySetCanceled(token));
         }
      }

      private static IntPtr FindGameWindow()
      {
         var processes = Process.GetProcessesByName("RelicCardinal");
         try
         {
            IntPtr window = processes.Select(process => process.MainWindowHandle).FirstOrDefault(handle => handle != IntPtr.Zero);
            if (window == IntPtr.Zero) throw new InvalidOperationException("没有找到游戏窗口。");
            return window;
         }
         finally { foreach (var process in processes) process.Dispose(); }
      }

      private static async Task PressAsync(IntPtr window, int key, bool systemKey, CancellationToken token)
      {
         token.ThrowIfCancellationRequested();
         uint keyDown = systemKey ? 0x0104u : 0x0100u;
         uint keyUp = systemKey ? 0x0105u : 0x0101u;
         int flags = 1 | ((int)MapVirtualKey((uint)key, 0) << 16);
         Send(window, keyDown, key, flags);
         try { await Task.Delay(15, token).ConfigureAwait(false); }
         finally
         {
            // 取消期间仍完成已发送按键的释放，不改变系统全局键盘状态。
            Send(window, keyUp, key, flags | unchecked((int)0xC0000000));
         }
         await Task.Delay(15, token).ConfigureAwait(false);
      }

      private static void Send(IntPtr window, uint message, int key, int flags)
      {
         Marshal.SetLastPInvokeError(0);
         if (SendMessageTimeout(window, message, (UIntPtr)(uint)key, (IntPtr)flags,
               0x0002 | 0x0020, 250, out _) == IntPtr.Zero)
         {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(error == 0
               ? "游戏窗口没有及时处理按键消息。"
               : "发送游戏按键失败：" + new Win32Exception(error).Message);
         }
      }

      [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
      private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, UIntPtr wParam,
         IntPtr lParam, uint flags, uint timeout, out UIntPtr result);

      [DllImport("user32.dll", EntryPoint = "MapVirtualKeyW")]
      private static extern uint MapVirtualKey(uint code, uint mapType);

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetForegroundWindow(IntPtr window);
   }
}
