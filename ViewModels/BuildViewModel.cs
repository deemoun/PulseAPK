using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using PulseAPK.Services;
using PulseAPK.Utils;

namespace PulseAPK.ViewModels
{
    public partial class BuildViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsHintVisible))]
        [NotifyPropertyChangedFor(nameof(HasProject))]
        private string _projectPath;

        [ObservableProperty]
        private string _outputApkPath;

        [ObservableProperty]
        private string _outputFolderPath;

        [ObservableProperty]
        private string _outputApkName;

        [ObservableProperty]
        private bool _useAapt2;

        [ObservableProperty]
        private string _consoleLog = Properties.Resources.WaitingForCommand;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunBuildCommand))]
        private bool _isRunning;

        private bool _isConsolePreviewActive = true;

        private readonly Services.IFilePickerService _filePickerService;
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.ApktoolRunner _apktoolRunner;

        public bool IsHintVisible => string.IsNullOrEmpty(ProjectPath);

        public bool HasProject => !string.IsNullOrWhiteSpace(ProjectPath);

        public BuildViewModel()
        {
            _filePickerService = new Services.FilePickerService();
            _settingsService = new Services.SettingsService();
            _apktoolRunner = new Services.ApktoolRunner(_settingsService);

            InitializeOutputPath();

            _apktoolRunner.OutputDataReceived += OnOutputDataReceived;

            UpdateCommandPreview();
            RunBuildCommand.NotifyCanExecuteChanged();
        }

        partial void OnProjectPathChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // Default output goes to the app's compiled folder using the project name.

                var sanitizedPath = value.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var folderName = Path.GetFileName(sanitizedPath);
                var compiledDir = EnsureCompiledDirectory();

                OutputFolderPath = compiledDir;
                OutputApkName = $"{folderName}.apk";
            }
            UpdateOutputApkPath();
            RunBuildCommand.NotifyCanExecuteChanged();
        }

        partial void OnOutputApkPathChanged(string value) => UpdateCommandPreview();
        partial void OnOutputFolderPathChanged(string value)
        {
            EnsureOutputFolderPathInitialized();

            UpdateOutputApkPath();
            BrowseOutputApkCommand.NotifyCanExecuteChanged();
        }
        partial void OnOutputApkNameChanged(string value)
        {
            UpdateOutputApkPath();
            RunBuildCommand.NotifyCanExecuteChanged();
        }
        partial void OnUseAapt2Changed(bool value) => UpdateCommandPreview();

        [RelayCommand]
        private void BrowseProject()
        {
            var folder = Utils.BrowseUtils.BrowseFolder(
                _filePickerService,
                ProjectPath,
                missingPathMessage: null,
                folderNotFoundMessage: Properties.Resources.Error_FolderNotFound,
                requireExistingBasePath: false);

            if (folder != null)
            {
                var (isValid, message) = Utils.FileSanitizer.ValidateProjectFolder(folder);
                if (!isValid)
                {
                    MessageBoxUtils.ShowWarning(message, Properties.Resources.Warning_InvalidProjectFolder);
                    return;
                }
                ProjectPath = folder;
            }
        }

        [RelayCommand(CanExecute = nameof(CanBrowseOutputApk))]
        private void BrowseOutputApk()
        {
            var folder = Utils.BrowseUtils.BrowseFolder(
                _filePickerService,
                OutputFolderPath,
                Properties.Resources.Error_OutputFolderNotSet,
                Properties.Resources.Error_FolderNotFound,
                requireExistingBasePath: true);

            if (!string.IsNullOrWhiteSpace(folder))
            {
                OutputFolderPath = folder;
            }
        }

        private bool CanBrowseOutputApk() => !string.IsNullOrWhiteSpace(OutputFolderPath);

        [RelayCommand(CanExecute = nameof(CanRunBuild))]
        private async Task RunBuild()
        {
            if (string.IsNullOrWhiteSpace(ProjectPath))
            {
             MessageBoxUtils.ShowWarning(Properties.Resources.SelectProjectFolder, Properties.Resources.BuildHeader);
                 return;
            }

            var apktoolPath = _settingsService.Settings.ApktoolPath?.Trim();
             if (string.IsNullOrWhiteSpace(apktoolPath) || !File.Exists(apktoolPath))
            {
                MessageBoxUtils.ShowError(string.Format(Properties.Resources.Error_InvalidApktoolPath, apktoolPath), Properties.Resources.SettingsHeader);
                return;
            }

            if (File.Exists(OutputApkPath))
            {
                 var result = MessageBoxUtils.ShowQuestion($"The output file '{OutputApkPath}' already exists. Overwrite?", "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                 if (result != MessageBoxResult.Yes) return;
            }

            SetConsoleLog(Properties.Resources.StartingApktool);
            IsRunning = true;

            try
            {
                var exitCode = await _apktoolRunner.RunBuildAsync(ProjectPath, OutputApkPath, UseAapt2);

                if (exitCode == 0)
                {
                    AppendLog(Properties.Resources.BuildSuccessful);
                     MessageBoxUtils.ShowInfo(Properties.Resources.BuildSuccessful);
                }
                else
                {
                     AppendLog($"{Properties.Resources.BuildFailed} (Exit Code: {exitCode})");
                     MessageBoxUtils.ShowError(Properties.Resources.BuildFailed);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"{Properties.Resources.BuildFailed}: {ex.Message}");
                MessageBoxUtils.ShowError(ex.Message);
            }
            finally
            {
                IsRunning = false;
                RunBuildCommand.NotifyCanExecuteChanged();
            }
        }

        private void OnOutputDataReceived(string message)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => AppendLog(message));
            }
            else
            {
                AppendLog(message);
            }
        }

        private void AppendLog(string message)
        {
            _isConsolePreviewActive = false;
            if (string.IsNullOrWhiteSpace(ConsoleLog) || ConsoleLog == "Waiting for command...")
            {
                ConsoleLog = message;
            }
            else
            {
                ConsoleLog += $"{Environment.NewLine}{message}";
            }
        }

        private void SetConsoleLog(string message)
        {
            _isConsolePreviewActive = false;
            ConsoleLog = message;
        }

        private void UpdateCommandPreview()
        {
            if (!_isConsolePreviewActive) return;
             ConsoleLog = BuildCommandPreview();
        }

        private bool CanRunBuild()
        {
            return !IsRunning
                && HasProject
                && !string.IsNullOrWhiteSpace(OutputApkName)
                && !string.IsNullOrWhiteSpace(OutputApkPath)
                && !Directory.Exists(OutputApkPath);
        }

        private string BuildCommandPreview()
        {
             var apktoolPath = _settingsService.Settings.ApktoolPath?.Trim();
            var apktool = string.IsNullOrWhiteSpace(apktoolPath) ? "<set apktool path>" : $"\"{apktoolPath}\"";
            var project = string.IsNullOrWhiteSpace(ProjectPath) ? "<select project>" : $"\"{ProjectPath}\"";
            var hasOutputPath = !string.IsNullOrWhiteSpace(OutputApkPath);
            var output = hasOutputPath ? $"\"{OutputApkPath}\"" : "<output apk>";

            var builder = new StringBuilder();
            builder.Append($"java -jar {apktool} b {project} -o {output}");
            if(UseAapt2) builder.Append(" --use-aapt2");

            return $"Command preview: {builder}";
        }

        private void InitializeOutputPath()
        {
            OutputFolderPath = EnsureCompiledDirectory();
            OutputApkName = string.Empty;
            OutputApkPath = OutputFolderPath;
        }

        private string EnsureCompiledDirectory()
        {
            var compiledDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "compiled");

            if (!Directory.Exists(compiledDir))
            {
                try { Directory.CreateDirectory(compiledDir); } catch { }
            }

            return compiledDir;
        }

        private string GetApplicationRootPath()
        {
            return string.IsNullOrWhiteSpace(AppDomain.CurrentDomain.BaseDirectory)
                ? Directory.GetCurrentDirectory()
                : AppDomain.CurrentDomain.BaseDirectory;
        }

        private void UpdateOutputApkPath()
        {
            var folderPath = EnsureOutputFolderPathInitialized();

            if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(OutputApkName))
            {
                OutputApkPath = string.IsNullOrWhiteSpace(folderPath) ? string.Empty : folderPath;
                UpdateCommandPreview();
                RunBuildCommand.NotifyCanExecuteChanged();
                return;
            }

            OutputApkPath = Path.Combine(folderPath, OutputApkName);
            UpdateCommandPreview();
            RunBuildCommand.NotifyCanExecuteChanged();
        }

        private string EnsureOutputFolderPathInitialized()
        {
            if (string.IsNullOrWhiteSpace(OutputFolderPath))
            {
                var defaultPath = EnsureCompiledDirectory();
                if (!string.Equals(OutputFolderPath, defaultPath, StringComparison.OrdinalIgnoreCase))
                {
                    OutputFolderPath = defaultPath;
                }
            }

            return OutputFolderPath;
        }
    }
}
