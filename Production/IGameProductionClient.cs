using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aoe4Helper.Production
{
   public interface IGameProductionClient
   {
      Task<List<RpcEntity>> DiscoverAsync(RpcSettings settings, CancellationToken token);
      Task<string> ProduceAsync(RpcSettings settings, string civilization, string building, int slot, int count, CancellationToken token);
      Task DisconnectAsync();
   }
}
