using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;

namespace APKToolUI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.IFilePickerService _filePickerService;
        private readonly bool _isInitialized;

        [ObservableProperty]
        private string _apktoolPath;

        public SettingsViewModel()
            : this(new Services.SettingsService(), new Services.FilePickerService())
        {
        }

        public SettingsViewModel(Services.ISettingsService settingsService, Services.IFilePickerService filePickerService)
        {
            _settingsService = settingsService;
            _filePickerService = filePickerService;

            ApktoolPath = _settingsService.Settings.ApktoolPath;

            _isInitialized = true;
        }

        [RelayCommand]
        private void BrowseApktool()
        {
            var file = _filePickerService.OpenFile("Jar Files (*.jar)|*.jar|Executable Files (*.exe)|*.exe|All Files (*.*)|*.*");
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            var extension = Path.GetExtension(file);
            if (extension.Equals(".jar", StringComparison.OrdinalIgnoreCase))
            {
                var (isValid, message) = Utils.FileSanitizer.ValidateJar(file);
                if (!isValid)
                {
                    System.Windows.MessageBox.Show(message, "Invalid Jar File", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
            }
            else if (!extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.MessageBox.Show("Please select a .jar or .exe file for apktool.", "Unsupported file", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
    }
}
