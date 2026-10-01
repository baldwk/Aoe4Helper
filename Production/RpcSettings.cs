using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Aoe4Helper.Production
{
   public sealed class RpcSettings
   {
      public string GameDirectory { get; set; } = FindGameDirectory();
      public string Host { get; set; } = "127.0.0.1";
      public int Port { get; set; } = 4602;
      public int LuaPort { get; set; } = 8081;
      public Dictionary<string, Dictionary<string, RpcBuildingMapping>> Civilizations { get; set; } = new();

      public static string FilePath => Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aoe4Helper", "rpc-settings.json");

      public static RpcSettings Load() => File.Exists(FilePath)
         ? JsonSerializer.Deserialize<RpcSettings>(File.ReadAllText(FilePath)) ?? new RpcSettings()
         : new RpcSettings();

      public void Save()
      {
         Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
         File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
      }

      public RpcSettings Copy() => JsonSerializer.Deserialize<RpcSettings>(JsonSerializer.Serialize(this));

      private static string FindGameDirectory()
      {
         using var steam = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
         var steamPath = steam?.GetValue("SteamPath") as string;
         if (string.IsNullOrEmpty(steamPath)) return "";
         var path = Path.Combine(steamPath, "steamapps", "common", "Age of Empires IV");
         return Directory.Exists(path) ? path : "";
      }
   }

   public sealed class RpcBuildingMapping
   {
      public string BuildingBlueprint { get; set; } = "";
      public string[] Units { get; set; } = new string[4];
   }
}
