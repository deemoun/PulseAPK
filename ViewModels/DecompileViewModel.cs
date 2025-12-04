using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

        public DecompileViewModel()
        {
            _filePickerService = new Services.FilePickerService();
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
            
            var runner = new Services.ApktoolRunner();
            await runner.RunDecompileAsync(ApkPath, outputDir, DecodeResources, DecodeSources);
        }
    }
}
