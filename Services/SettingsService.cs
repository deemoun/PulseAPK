using System;
using System.IO;
using System.Text.Json;

namespace PulseAPK.Services
{
    public class AppSettings
    {
        public string ApktoolPath { get; set; } = "apktool.jar";
        public string UbersignPath { get; set; } = "ubersign.jar";
    }

    public interface ISettingsService
    {
        AppSettings Settings { get; }
        void Save();
    }

    public class SettingsService : ISettingsService
    {
        private const string SettingsFileName = "settings.json";
        private readonly string _settingsFilePath;

        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            var settingsFolder = AppContext.BaseDirectory;
            _settingsFilePath = Path.Combine(settingsFolder, SettingsFileName);
            Settings = LoadSettings();
        }

        private AppSettings LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
                catch
                {
                    // Fallback to defaults when the settings file cannot be read
                }
            }

            return new AppSettings();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
    }
}
