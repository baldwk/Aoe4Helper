using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Aoe4Helper.Production
{
   public sealed class LuaProductionClient : IGameProductionClient
   {
      private readonly SemaphoreSlim serial = new(1, 1);
      private readonly LuaDebuggerClient debugger = new();
      private readonly string mailbox = Path.Combine(Path.GetDirectoryName(RpcSettings.FilePath), "lua", Guid.NewGuid().ToString("N") + ".lua");
      private bool installed;
      private int gameProcessId;
      private string connectionKey;
      private string logFile;

      public async Task<List<RpcEntity>> DiscoverAsync(RpcSettings settings, CancellationToken token)
      {
         await serial.WaitAsync(token).ConfigureAwait(false);
         try
         {
            var catalog = await InstallAsync(settings, token).ConfigureAwait(false);
            foreach (var entity in catalog.Entities)
            {
               // 这是当前文明的候选蓝图列表，具体建筑能否训练由游戏在提交时决定。
               entity.ProductionQueue = new RpcProductionQueue
               {
                  ProduceableItems = catalog.UnitNames.Distinct().Select(name => new RpcProduceableItem
                  {
                     Name = name, Type = RpcProduceableItem.Spawn
                  }).ToList()
               };
            }
            return catalog.Entities;
         }
         finally { serial.Release(); }
      }

      public async Task<string> ProduceAsync(RpcSettings settings, string civilization, string building,
         int slot, int count, CancellationToken token)
      {
         token.ThrowIfCancellationRequested();
         if (count <= 0) return $"{building}：成功入队 0/0。";
         if (!ProductionMapping.TryResolve(settings, civilization, building, slot, out string blueprint, out string unit))
            return $"请在生产设置中配置 {civilization} / {building} / {ProductionMapping.SlotKey(slot)}。";

         await serial.WaitAsync(token).ConfigureAwait(false);
         bool requestWritten = false;
         bool failed = false;
         try
         {
            if (!installed || gameProcessId != CurrentGameProcessId() || connectionKey != Key(settings))
               await InstallAsync(settings, token).ConfigureAwait(false);

            using var log = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            log.Seek(0, SeekOrigin.End);
            using var reader = new StreamReader(log, Encoding.UTF8, false, 4096, true);
            string id = Guid.NewGuid().ToString("N");
            token.ThrowIfCancellationRequested();
            WriteMailbox("return {id=" + LuaDebuggerClient.Quote(id) + ",kind=\"produce\",building=" +
               LuaDebuggerClient.Quote(blueprint) + ",unit=" + LuaDebuggerClient.Quote(unit) +
               ",count=" + count + "}");
            requestWritten = true;
            var timer = Stopwatch.StartNew();
            var received = new StringBuilder();
            string marker = "AOE4HELPER_RESULT|" + id + "|";
            while (timer.Elapsed < TimeSpan.FromSeconds(8))
            {
               token.ThrowIfCancellationRequested();
               received.Append(await reader.ReadToEndAsync(token).ConfigureAwait(false));
               string text = received.ToString();
               int start = text.IndexOf(marker, StringComparison.Ordinal);
               int end = start < 0 ? -1 : text.IndexOf('\n', start);
               if (end >= 0)
               {
                  string[] fields = text.Substring(start + marker.Length, end - start - marker.Length).TrimEnd('\r').Split('|');
                  if (fields[0] == "ERROR") throw new InvalidOperationException(string.Join("|", fields.Skip(1)));
                  int accepted = int.Parse(fields[1]);
                  return $"{building}：成功入队 {accepted}/{count}。" + (accepted < count ? "其余请求未被游戏接受。" : "");
               }
               await Task.Delay(50, token).ConfigureAwait(false);
            }
            installed = false;
            throw new TimeoutException("游戏内脚本未返回结果，请保持对局运行并重新连接。未重发本次请求。");
         }
         catch
         {
            failed = true;
            throw;
         }
         finally
         {
            try { if (requestWritten) WriteMailbox("return {id=" + LuaDebuggerClient.Quote(Guid.NewGuid().ToString("N")) + ",kind=\"idle\"}"); }
            catch (Exception ex) when (failed && (ex is IOException || ex is UnauthorizedAccessException))
            {
               installed = false;
               // 保留生产失败或取消的原始异常。
            }
            finally { serial.Release(); }
         }
      }

      private async Task<LuaCatalog> InstallAsync(RpcSettings settings, CancellationToken token)
      {
         installed = false;
         WriteMailbox("return {id=\"initial\",kind=\"idle\"}");
         string assemblyDirectory = Path.GetDirectoryName(typeof(LuaProductionClient).Assembly.Location);
         string script = Path.Combine(string.IsNullOrEmpty(assemblyDirectory) ? AppContext.BaseDirectory : assemblyDirectory,
            "Production", "Lua", "production_bridge.lua");
         string expression = "assert(loadfile(" + LuaDebuggerClient.Quote(script.Replace('\\', '/')) + "))()(" +
            LuaDebuggerClient.Quote(mailbox.Replace('\\', '/')) + ")";
         string json = await debugger.EvaluateAsync(settings, expression, token).ConfigureAwait(false);
         var catalog = JsonSerializer.Deserialize<LuaCatalog>(json);
         string logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Age of Empires IV", "LogFiles");
         logFile = Directory.EnumerateFiles(logs, "scarlog*.txt", SearchOption.AllDirectories)
            .OrderByDescending(File.GetCreationTimeUtc).FirstOrDefault();
         if (logFile == null) throw new FileNotFoundException("没有找到本次游戏的 scarlog，请使用 -dev 启动游戏。");
         gameProcessId = CurrentGameProcessId();
         connectionKey = Key(settings);
         installed = true;
         return catalog;
      }

      private void WriteMailbox(string source)
      {
         Directory.CreateDirectory(Path.GetDirectoryName(mailbox));
         string temporary = mailbox + ".tmp";
         File.WriteAllText(temporary, source, new UTF8Encoding(false));
         File.Move(temporary, mailbox, true);
      }

      private static string Key(RpcSettings settings) => settings.Host + ":" + settings.LuaPort;

      private static int CurrentGameProcessId()
      {
         var processes = Process.GetProcessesByName("RelicCardinal");
         try { return processes.FirstOrDefault()?.Id ?? 0; }
         finally { foreach (var process in processes) process.Dispose(); }
      }

      public async Task DisconnectAsync()
      {
         await serial.WaitAsync().ConfigureAwait(false);
         try
         {
            if (File.Exists(mailbox)) WriteMailbox("return {id=" + LuaDebuggerClient.Quote(Guid.NewGuid().ToString("N")) + ",kind=\"stop\"}");
            installed = false;
         }
         finally { serial.Release(); }
      }

      public sealed class LuaCatalog
      {
         public string Race { get; set; }
         public List<string> UnitNames { get; set; }
         public List<RpcEntity> Entities { get; set; }
      }
   }
}
