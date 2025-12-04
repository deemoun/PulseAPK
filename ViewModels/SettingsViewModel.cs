using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace APKToolUI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.IFilePickerService _filePickerService;

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
        }

        [RelayCommand]
        private void BrowseApktool()
        {
            var file = _filePickerService.OpenFile("Jar Files (*.jar)|*.jar|Executable Files (*.exe)|*.exe|All Files (*.*)|*.*");
            if (file != null)
            {
                ApktoolPath = file;
            }
        }

        [RelayCommand]
        private void SaveSettings()
        {
            _settingsService.Settings.ApktoolPath = ApktoolPath?.Trim() ?? string.Empty;
            _settingsService.Save();
        }
    }
}
