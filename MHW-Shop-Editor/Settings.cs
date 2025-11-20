using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MHWShopEditor
{
    public class Settings
    {
        private static string SettingsPath = "settings.json";
        
        public string SaveDirectory { get; set; } = "";
        public string Language { get; set; } = "eng";
        public bool IsInsertTop { get; set; } = false; // Default false means Bottom (insert = -1)
        public double AppFontSize { get; set; } = 19.6; // Default 1.4x of standard 14
        public List<string> SavedItems { get; set; } = new List<string>();

        public static Settings Default { get; private set; } = new Settings();

        static Settings()
        {
            Load();
        }

        public static void Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    var loaded = JsonSerializer.Deserialize<Settings>(json);
                    if (loaded != null)
                        Default = loaded;
                }
                catch { }
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
namespace MHWShopEditor.Properties 
{
    // Compatibility wrapper
    public static class Settings 
    {
        public static MHWShopEditor.Settings Default => MHWShopEditor.Settings.Default;
    }
}