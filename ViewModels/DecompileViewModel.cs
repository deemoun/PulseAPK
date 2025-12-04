using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace APKToolUI.ViewModels
{
    public partial class DecompileViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _apkPath;

        [ObservableProperty]
        private bool _decodeResources = true;

        [ObservableProperty]
        private bool _decodeSources = true;

        private readonly Services.IFilePickerService _filePickerService;
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.ApktoolRunner _apktoolRunner;

        public DecompileViewModel()
        {
            _filePickerService = new Services.FilePickerService();
            _settingsService = new Services.SettingsService();
            _apktoolRunner = new Services.ApktoolRunner(_settingsService);
        }

        [RelayCommand]
        private void BrowseApk()
        {
            var file = _filePickerService.OpenFile("APK Files (*.apk)|*.apk|All Files (*.*)|*.*");
            if (file != null)
            {
                ApkPath = file;
            }
        }

        [RelayCommand]
        private async Task RunDecompile()
        {
            if (string.IsNullOrEmpty(ApkPath)) return;

            // TODO: Get output folder from UI or default to APK folder
            var outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ApkPath)!, System.IO.Path.GetFileNameWithoutExtension(ApkPath));

            await _apktoolRunner.RunDecompileAsync(ApkPath, outputDir, DecodeResources, DecodeSources);
        }
    }
}
