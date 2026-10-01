using Aoe4Helper.Production;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Aoe4Helper
{
   public partial class MainWindow
   {
      private readonly WindowMessageProductionClient windowInput = new();
      private readonly Dictionary<string, CancellationToken> productionPending = new();
      private readonly Dictionary<string, CancellationTokenSource> buildingCancellation = new();
      private CancellationTokenSource productionCancellation = new();

      private void CancelProductionRequests(string building = null)
      {
         if (building != null)
         {
            if (buildingCancellation.Remove(building, out var source))
            {
               source.Cancel();
               source.Dispose();
            }
            return;
         }
         productionCancellation.Cancel();
         foreach (var source in buildingCancellation.Values) source.Dispose();
         buildingCancellation.Clear();
         productionCancellation.Dispose();
         productionCancellation = new CancellationTokenSource();
      }

      private async Task ProduceWithKeyboardAsync(string building, int slot, int count, bool activateGame = false)
      {
         string key = $"{civ}/{building}/{slot}";
         if (productionPending.TryGetValue(key, out var previous) && !previous.IsCancellationRequested) return;
         if (!buildingCancellation.TryGetValue(building, out var source))
         {
            source = CancellationTokenSource.CreateLinkedTokenSource(productionCancellation.Token);
            buildingCancellation[building] = source;
         }
         var token = source.Token;
         productionPending[key] = token;
         try
         {
            await windowInput.ProduceAsync(building, slot, count, token, activateGame);
            if (!token.IsCancellationRequested)
            {
               ProductionStatus.Text = "";
               ProductionStatus.Visibility = Visibility.Collapsed;
            }
         }
         catch (OperationCanceledException) { }
         catch (Exception ex)
         {
            if (!token.IsCancellationRequested) ShowProductionError($"生产：{ex.Message}");
         }
         finally
         {
            if (productionPending.TryGetValue(key, out var current) && current == token) productionPending.Remove(key);
         }
      }

      private void ShowProductionError(string message)
      {
         ProductionStatus.Text = message;
         ProductionStatus.Visibility = Visibility.Visible;
      }
   }
}
