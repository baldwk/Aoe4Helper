using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Aoe4Helper.Production
{
   /// <summary>使用游戏附带的 RPC 客户端；游戏 DLL 在运行时从安装目录加载。</summary>
   public sealed class RpcProductionClient : IGameProductionClient
   {
      private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
      private readonly SemaphoreSlim serial = new(1, 1);
      private dynamic channel;
      private Type archiveType;
      private string endpoint;

      public Task<List<RpcEntity>> DiscoverAsync(RpcSettings settings, CancellationToken token) =>
         InSessionAsync(settings, () => GetPlayerBuildings(), token);

      public Task<string> ProduceAsync(RpcSettings settings, string civilization, string building,
         int slot, int count, CancellationToken token)
      {
         token.ThrowIfCancellationRequested();
         if (count <= 0) return Task.FromResult($"{building}：已提交 0 个生产请求。");
         if (!ProductionMapping.TryResolve(settings, civilization, building, slot, out string blueprint, out string unit))
         {
            return Task.FromResult($"请在生产设置中配置 {civilization} / {building} / {ProductionMapping.SlotKey(slot)}。");
         }

         return InSessionAsync(settings, () =>
         {
            int sent = 0;
            // 同一批次内保留已提交的数量，补偿游戏队列状态可能延后一帧更新。
            var assignedQueueSizes = new Dictionary<uint, int>();
            try
            {
               while (sent < count)
               {
                  token.ThrowIfCancellationRequested();
                  var candidates = GetPlayerBuildings()
                     .Where(entity => string.Equals(entity.BlueprintName, blueprint, StringComparison.OrdinalIgnoreCase))
                     .Select(entity => new
                     {
                        Entity = entity,
                        Item = entity.ProductionQueue.ProduceableItems?.FirstOrDefault(item =>
                           item.Type == RpcProduceableItem.Spawn && item.Name == unit && item.CanBeProduced),
                        QueueSize = Math.Max(entity.ProductionQueue.QueuedItems?.Count ?? 0,
                           assignedQueueSizes.TryGetValue(entity.EntityId, out int size) ? size : 0)
                     })
                     .Where(candidate => candidate.Item != null)
                     .OrderBy(candidate => candidate.QueueSize)
                     .ThenBy(candidate => candidate.Entity.EntityId)
                     .ToList();

                  var target = candidates.FirstOrDefault();
                  if (target == null) return $"{building}：已提交 {sent}/{count}，当前没有可生产该单位的建筑。";

                  token.ThrowIfCancellationRequested();
                  Call("EntityQueueBuildItem", archive =>
                  {
                     archive.SetNamedValue<uint>("EntityId", target.Entity.EntityId);
                     archive.SetNamedValue<uint>("ItemId", target.Item.Id);
                     archive.SetNamedValue<byte>("ItemType", target.Item.Type);
                  });
                  sent++;
                  assignedQueueSizes[target.Entity.EntityId] = target.QueueSize + 1;
               }
               return $"{building}：已提交 {sent} 个生产请求。";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
               // 请求结果不明时不重发，避免同一次生产被重复提交。
               throw new InvalidOperationException($"{building}：已提交 {sent}/{count}；后续请求失败：{ex.GetBaseException().Message}", ex);
            }
         }, token);
      }

      private List<RpcEntity> GetPlayerBuildings()
      {
         dynamic localPlayer = Call("GetLocalPlayerId");
         uint playerId = localPlayer.GetNamedValue<uint>("PlayerId");
         dynamic result = Call("GetPlayerEntities", archive =>
         {
            archive.SetNamedValue<uint>("PlayerId", playerId);
            archive.SetNamedValue<uint>("Flags", uint.MaxValue); // EntityFlags.All
         });
         var entities = (IEnumerable<RpcEntity>)result.GetNamedCollection<RpcEntity>("Entities");
         return entities.Where(entity => entity.OwnerId == playerId && entity.ProductionQueue != null).ToList();
      }

      private object Call(string method, Action<dynamic> setParameters = null)
      {
         dynamic archive = Activator.CreateInstance(archiveType);
         setParameters?.Invoke(archive);
         return channel.InvokeRemoteMethod(method, archive, Timeout);
      }

      private async Task<T> InSessionAsync<T>(RpcSettings settings, Func<T> action, CancellationToken token)
      {
         await serial.WaitAsync(token).ConfigureAwait(false);
         try
         {
            return await Task.Run(() =>
            {
               try
               {
                  Connect(settings);
                  token.ThrowIfCancellationRequested();
                  return action();
               }
               catch (OperationCanceledException) { throw; }
               catch
               {
                  CloseChannel();
                  throw;
               }
            }, token).ConfigureAwait(false);
         }
         finally { serial.Release(); }
      }

      private void Connect(RpcSettings settings)
      {
         string key = $"{settings.GameDirectory}|{settings.Host}|{settings.Port}";
         if (channel != null && endpoint == key && channel.IsConnected) return;
         CloseChannel();

         string dll = Path.GetFullPath(Path.Combine(settings.GameDirectory, "GamesTest.Rpc.Client.dll"));
         if (!File.Exists(dll)) throw new FileNotFoundException("请选择包含 GamesTest.Rpc.Client.dll 的游戏安装目录。", dll);
         Assembly assembly = Assembly.LoadFrom(dll);
         archiveType = assembly.GetType("Microsoft.Internal.GamesTest.Rpc.Client.RpcArchive", true);
         Type channelType = assembly.GetType("Microsoft.Internal.GamesTest.Rpc.Client.RpcChannel", true);
         IPAddress address = IPAddress.TryParse(settings.Host, out var parsed)
            ? parsed : Dns.GetHostAddresses(settings.Host).First();
         channel = Activator.CreateInstance(channelType, address, settings.Port);
         channel.OperationTimeout = Timeout;
         channel.Connect(Timeout);
         endpoint = key;
      }

      public async Task DisconnectAsync()
      {
         await serial.WaitAsync().ConfigureAwait(false);
         try { await Task.Run(() => CloseChannel()).ConfigureAwait(false); }
         finally { serial.Release(); }
      }

      private void CloseChannel()
      {
         if (channel is IDisposable disposable)
         {
            try { disposable.Dispose(); }
            catch { /* 关闭失效连接时保留原始调用错误。 */ }
         }
         channel = null;
         endpoint = null;
      }
   }
}
