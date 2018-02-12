using System;
using System.IO;
using Newtonsoft.Json;

namespace esnew
{

    public class Settings
    {
        public Guid DeviceGuid { get; set; }

        public static Settings Instance { get; private set; }

        public static void Initialize()
        {
            if (File.Exists(SettingsPath))
            {
                Instance = JsonConvert.DeserializeObject<Settings>(
                    File.ReadAllText(SettingsPath));
            }
            else 
            {
                Instance = new Settings();
                Instance.DeviceGuid = Guid.NewGuid();
                Save();
            }
        }

        public static void Save()
        {
            File.WriteAllText(SettingsPath, 
                JsonConvert.SerializeObject(Instance, Formatting.Indented));
        }

        private static string SettingsPath {
            get 
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
            }
        }
    }

}