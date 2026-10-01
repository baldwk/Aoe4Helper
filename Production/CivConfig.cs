using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Aoe4Helper
{
   // 保持 prod.tab 的现有字段名；字符串只在加载时解析一次。
   public sealed class CivConfig
   {
      public string tc { get; set; }
      public string a_q { get; set; }
      public string a_w { get; set; }
      public string a_e { get; set; }
      public string a_r { get; set; }
      public string s_q { get; set; }
      public string s_w { get; set; }
      public string s_e { get; set; }
      public string s_r { get; set; }
      public string b_q { get; set; }
      public string b_w { get; set; }
      public string b_e { get; set; }
      public string b_r { get; set; }
      public string b_t { get; set; }
      public string food { get; set; }
      public string wood { get; set; }
      public string gold { get; set; }
      public string stone { get; set; }

      internal CivilizationProductionConfig Parse(string civilization)
      {
         try
         {
            return new CivilizationProductionConfig(new Dictionary<BuildingType, UnitProductionConfig[]>
            {
               [BuildingType.TownCenter] = ParseSlots(tc),
               [BuildingType.Archery] = ParseSlots(a_q, a_w, a_e, a_r),
               [BuildingType.Stable] = ParseSlots(s_q, s_w, s_e, s_r),
               [BuildingType.Barracks] = ParseSlots(b_q, b_w, b_e, b_r, b_t)
            }, new[] { food, wood, gold }.Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray());
         }
         catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is OverflowException)
         {
            throw new InvalidDataException($"prod.tab 中 {civilization} 的成本、训练时间或采集效率格式错误。", ex);
         }
      }

      private static UnitProductionConfig[] ParseSlots(params string[] entries) =>
         entries.Select(UnitProductionConfig.Parse).ToArray();
   }

   internal sealed class CivilizationProductionConfig
   {
      public Dictionary<BuildingType, UnitProductionConfig[]> Slots { get; }
      public double[] GatheringRates { get; }

      public CivilizationProductionConfig(Dictionary<BuildingType, UnitProductionConfig[]> slots, double[] gatheringRates)
      {
         if (gatheringRates.Any(rate => !double.IsFinite(rate) || rate <= 0))
            throw new FormatException("采集效率必须为正数。");
         Slots = slots;
         GatheringRates = gatheringRates;
      }
   }

   internal sealed class UnitProductionConfig
   {
      public double[] Costs { get; }
      public double TrainingSeconds { get; }

      private UnitProductionConfig(double[] values)
      {
         Costs = values.Take(4).ToArray();
         TrainingSeconds = values[4];
      }

      public static UnitProductionConfig Parse(string entry)
      {
         if (string.IsNullOrWhiteSpace(entry)) return new UnitProductionConfig(new double[5]);
         var values = entry.Split(',').Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();
         if (values.Length != 5 || values.Any(value => !double.IsFinite(value) || value < 0))
            throw new FormatException("生产槽位需要四项非负成本和训练秒数。");
         return new UnitProductionConfig(values);
      }
   }
}
