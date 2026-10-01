using System;

namespace Aoe4Helper.Production
{
   internal static class ProductionMapping
   {
      public static char SlotKey(int slot)
      {
         const string keys = "QWERT";
         if (slot < 0 || slot >= keys.Length) throw new ArgumentOutOfRangeException(nameof(slot));
         return keys[slot];
      }

      public static bool TryResolve(RpcSettings settings, string civilization, string building, int slot,
         out string blueprint, out string unit)
      {
         SlotKey(slot);
         blueprint = unit = null;
         if (settings?.Civilizations == null || !settings.Civilizations.TryGetValue(civilization, out var mappings) ||
             mappings == null || !mappings.TryGetValue(building, out var mapping) ||
             string.IsNullOrWhiteSpace(mapping?.BuildingBlueprint) || mapping.Units == null ||
             slot >= mapping.Units.Length || string.IsNullOrWhiteSpace(mapping.Units[slot])) return false;
         blueprint = mapping.BuildingBlueprint;
         unit = mapping.Units[slot];
         return true;
      }
   }
}
