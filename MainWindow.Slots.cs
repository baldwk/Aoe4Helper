using AduSkin.Controls.Metro;
using System.Windows;

namespace Aoe4Helper
{
   public partial class MainWindow
   {
      private void UpdateProductionSlotVisibility(CivilizationProductionConfig config)
      {
         SetSlotVisibility(ArcheryESlot, ArcheryECount, BuildingType.Archery, 2, config);
         SetSlotVisibility(ArcheryRSlot, ArcheryRCount, BuildingType.Archery, 3, config);
         SetSlotVisibility(StableESlot, StableECount, BuildingType.Stable, 2, config);
         SetSlotVisibility(StableRSlot, StableRCount, BuildingType.Stable, 3, config);
         SetSlotVisibility(BarracksESlot, BarracksECount, BuildingType.Barracks, 2, config);
         SetSlotVisibility(BarracksRSlot, BarracksRCount, BuildingType.Barracks, 3, config);
         SetSlotVisibility(BarracksTSlot, BarracksTCount, BuildingType.Barracks, 4, config);
      }

      private void SetSlotVisibility(FrameworkElement panel, AduIntegerUpDown input,
         BuildingType type, int index, CivilizationProductionConfig config)
      {
         bool available = (config?.Slots[type][index].TrainingSeconds ?? 0) > 0;
         panel.Visibility = available ? Visibility.Visible : Visibility.Collapsed;
         if (!available)
         {
            buildings[type].Quantities[index] = 0;
            if (input.Value != 0) input.Value = 0;
         }
      }
   }
}
