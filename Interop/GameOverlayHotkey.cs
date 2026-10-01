using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Aoe4Helper.Interop
{
   /// <summary>在游戏或助手前台时处理单独的 P，其他应用的按键原样传递。</summary>
   public sealed class GameOverlayHotkey : IDisposable
   {
      private readonly HookProc callback;
      private readonly Action<IntPtr> toggle;
      private readonly Dispatcher hookDispatcher;
      private IntPtr hook;
      private bool keyDown;
      private bool suppressPress;
      private int disposed;

      public GameOverlayHotkey(Action<IntPtr> toggle)
      {
         this.toggle = toggle;
         callback = HandleKeyboard;
         var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
         var thread = new Thread(() => RunHook(ready))
         {
            IsBackground = true,
            Name = "Aoe4Helper P hotkey"
         };
         thread.SetApartmentState(ApartmentState.STA);
         thread.Start();
         hookDispatcher = ready.Task.GetAwaiter().GetResult();
      }

      private void RunHook(TaskCompletionSource<Dispatcher> ready)
      {
         Dispatcher dispatcher;
         try
         {
            dispatcher = Dispatcher.CurrentDispatcher;
            hook = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
         }
         catch (Exception ex)
         {
            ready.SetException(ex);
            return;
         }

         try
         {
            ready.SetResult(dispatcher);
            // 全局键盘回调在安装钩子的线程执行，独立消息循环避免等待 WPF 界面绘制。
            Dispatcher.Run();
         }
         finally
         {
            UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
         }
      }

      private IntPtr HandleKeyboard(int code, IntPtr message, IntPtr data)
      {
         if (code < 0 || Volatile.Read(ref disposed) != 0 || Marshal.ReadInt32(data) != 0x50)
            return CallNextHookEx(hook, code, message, data);

         int keyMessage = message.ToInt32();
         if (keyMessage == 0x0101 || keyMessage == 0x0105)
         {
            bool suppress = suppressPress;
            keyDown = suppressPress = false;
            return suppress ? (IntPtr)1 : CallNextHookEx(hook, code, message, data);
         }

         if (keyMessage == 0x0100 || keyMessage == 0x0104)
         {
            if (!keyDown)
            {
               keyDown = true;
               suppressPress = false;
               // 这里只读取已经按住的修饰键，不用异步状态判断本次 P 事件。
               bool modified = IsPressed(0x10) || IsPressed(0x11) || IsPressed(0x12) || IsPressed(0x5B) || IsPressed(0x5C);
               if (!modified && IsTargetForeground(out IntPtr window))
               {
                  try
                  {
                     toggle(window); // 调用方只将显隐操作排入 Dispatcher，立即返回钩子。
                     suppressPress = true;
                  }
                  catch (InvalidOperationException) { }
               }
            }
            if (suppressPress) return (IntPtr)1;
         }
         return CallNextHookEx(hook, code, message, data);
      }

      private static bool IsTargetForeground(out IntPtr window)
      {
         window = GetForegroundWindow();
         GetWindowThreadProcessId(window, out uint id);
         if (id == Environment.ProcessId) return true;
         if (id == 0) return false;
         try
         {
            using var process = Process.GetProcessById((int)id);
            return string.Equals(process.ProcessName, "RelicCardinal", StringComparison.OrdinalIgnoreCase);
         }
         catch (ArgumentException) { return false; }
         catch (InvalidOperationException) { return false; }
         catch (Win32Exception) { return false; }
      }

      private static bool IsPressed(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

      public void Dispose()
      {
         if (Interlocked.Exchange(ref disposed, 1) != 0) return;
         // 在安装线程退出消息循环并卸载钩子，关闭窗口时无需同步等待。
         hookDispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
      }

      private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

      [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
      private static extern IntPtr SetWindowsHookEx(int type, HookProc callback, IntPtr module, uint thread);
      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool UnhookWindowsHookEx(IntPtr hook);
      [DllImport("user32.dll")]
      private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
      [DllImport("user32.dll")]
      private static extern IntPtr GetForegroundWindow();
      [DllImport("user32.dll")]
      private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
      [DllImport("user32.dll")]
      private static extern short GetAsyncKeyState(int key);
      [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
      private static extern IntPtr GetModuleHandle(string module);
   }
}
