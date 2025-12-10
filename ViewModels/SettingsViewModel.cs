using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using PulseAPK.Utils;

namespace PulseAPK.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.IFilePickerService _filePickerService;
        private readonly bool _isInitialized;

        [ObservableProperty]
        private string _apktoolPath;

        [ObservableProperty]
        private string _ubersignPath;

        [ObservableProperty]
        private string _javaPathDisplay;

        public SettingsViewModel()
            : this(new Services.SettingsService(), new Services.FilePickerService())
        {
        }

        public SettingsViewModel(Services.ISettingsService settingsService, Services.IFilePickerService filePickerService)
        {
            _settingsService = settingsService;
            _filePickerService = filePickerService;

            ApktoolPath = _settingsService.Settings.ApktoolPath;
            UbersignPath = _settingsService.Settings.UbersignPath;
            JavaPathDisplay = GetJavaPathDisplay();

            _isInitialized = true;
        }

        [RelayCommand]
        private void BrowseApktool()
        {
            var file = _filePickerService.OpenFile(Properties.Resources.FileFilter_Jar);
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            var (isValid, message) = Utils.FileSanitizer.ValidateJar(file);
            if (!isValid)
            {
                MessageBoxUtils.ShowError(message, Properties.Resources.Error_InvalidJarFile);
                return;
            }

            ApktoolPath = file;
        }

        partial void OnApktoolPathChanged(string value)
        {
            if (!_isInitialized)
            {
                return;
            }

            _settingsService.Settings.ApktoolPath = value?.Trim() ?? string.Empty;
            _settingsService.Save();
        }

        [RelayCommand]
        private void BrowseUbersign()
        {
            var file = _filePickerService.OpenFile(Properties.Resources.FileFilter_Ubersign);
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            var (isValid, message) = Utils.FileSanitizer.ValidateUbersign(file);
            if (!isValid)
            {
                MessageBoxUtils.ShowError(message, Properties.Resources.Error_InvalidUbersignFile);
                return;
            }

            UbersignPath = file;
        }

        partial void OnUbersignPathChanged(string value)
        {
            if (!_isInitialized)
            {
                return;
            }

            _settingsService.Settings.UbersignPath = value?.Trim() ?? string.Empty;
            _settingsService.Save();
        }

        private static string GetJavaPathDisplay()
        {
            var javaPath = FindJavaExecutable();
            if (string.IsNullOrWhiteSpace(javaPath))
            {
                return Properties.Resources.JavaPathNotFound;
            }

            return string.Format(Properties.Resources.JavaPathFound, javaPath);
        }

        private static string FindJavaExecutable()
        {
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                var javaHomePath = Path.Combine(javaHome, "bin", "java.exe");
                if (File.Exists(javaHomePath))
                {
                    return javaHomePath;
                }
            }

            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
            foreach (var path in paths)
            {
                var trimmedPath = path.Trim();
                if (string.IsNullOrWhiteSpace(trimmedPath))
                {
                    continue;
                }

                var javaPath = Path.Combine(trimmedPath, "java.exe");
                if (File.Exists(javaPath))
                {
                    return javaPath;
                }
            }

            return string.Empty;
        }
    }
}
