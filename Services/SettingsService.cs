using System;
using System.IO;
using System.Text.Json;

namespace APKToolUI.Services
{
    public class AppSettings
    {
        public string ApktoolPath { get; set; } = "apktool.jar";
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
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Stores settings under %AppData%/APKToolUI (Windows) or ~/.config/APKToolUI (Linux/macOS)
            var settingsFolder = Path.Combine(appData, "APKToolUI");
            Directory.CreateDirectory(settingsFolder);

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
