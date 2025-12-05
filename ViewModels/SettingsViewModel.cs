using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            if (file != null)
            {
                var (isValid, message) = Utils.FileSanitizer.ValidateJar(file);
                if (!isValid)
                {
                    // If the user selected an EXE, we might want to allow it if it's a wrapper, but the request was "sanitation of the jar file".
                    // The file filter allows *.exe. If it is an EXE, ValidateJar (checking for ZIP magic) might fail or pass depending on the EXE.
                    // However, standard apktool is a JAR.
                    // If the user picked an EXE, ValidateJar will reject it if it doesn't have a ZIP header (which EXEs usually don't, they implement PE).
                    // Let's assume strict JAR requirement based on "sanitation of the jar file".
                    // But wait, the filter is "Jar Files (*.jar)|*.jar|Executable Files (*.exe)|*.exe".
                    // If the user selects an EXE, should I reject it? The user asked for "sanitation of the jar file".
                    // If they select an EXE, they are not selecting a JAR.
                    // I will check the extension first. If it is .exe, I might let it pass or warn?
                    // The prompt was specific: "sanitation of the jar file".
                    // I'll stick to validating if it is a .jar. If it is an .exe, I will skip ValidateJar or handle it differently.
                    // But for now, I will assume if they pick a .jar, it must be a valid .jar.

                    if (file.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!isValid)
                        {
                            // It failed validation
                            System.Windows.MessageBox.Show(message, "Invalid Jar File", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            return;
                        }
                    }
                    // If it is an EXE, we presently don't have instructions to sanitize it, so we accept it as before.

                    ApktoolPath = file;
                }
            }
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
