using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using PulseAPK.Services;

namespace PulseAPK.ViewModels
{
    public partial class BuildViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsHintVisible))]
        private string _projectPath;

        [ObservableProperty]
        private string _outputApkPath;

        [ObservableProperty]
        private bool _useAapt2;

        [ObservableProperty]
        private bool _forceAll;

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

        public BuildViewModel()
        {
            _filePickerService = new Services.FilePickerService();
            _settingsService = new Services.SettingsService();
            _apktoolRunner = new Services.ApktoolRunner(_settingsService);

            _apktoolRunner.OutputDataReceived += OnOutputDataReceived;
            
            UpdateCommandPreview();
            RunBuildCommand.NotifyCanExecuteChanged();
        }

        partial void OnProjectPathChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // Logic based on user request:
                // "Itstead of 'Output Folder' it should just be the current folder of the application + 'compiled' folder. It fill just create the file there"
                // "The name of the APK file should be the name of the current folder" (project name)
                
                var sanitizedPath = value.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var folderName = Path.GetFileName(sanitizedPath);
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var compiledDir = Path.Combine(appDir, "compiled");
                
                // Ensure directory exists
                if (!Directory.Exists(compiledDir))
                {
                    try { Directory.CreateDirectory(compiledDir); } catch { }
                }

                OutputApkPath = Path.Combine(compiledDir, $"{folderName}.apk");
            }
            UpdateCommandPreview();
            RunBuildCommand.NotifyCanExecuteChanged();
        }

        partial void OnOutputApkPathChanged(string value) => UpdateCommandPreview();
        partial void OnUseAapt2Changed(bool value) => UpdateCommandPreview();
        partial void OnForceAllChanged(bool value) => UpdateCommandPreview();

        [RelayCommand]
        private void BrowseProject()
        {
            var folder = _filePickerService.OpenFolder();
            if (folder != null)
            {
                var (isValid, message) = Utils.FileSanitizer.ValidateProjectFolder(folder);
                if (!isValid)
                {
                    MessageBox.Show(message, Properties.Resources.Warning_InvalidProjectFolder, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ProjectPath = folder;
            }
        }

        [RelayCommand]
        private void BrowseOutputApk()
        {
            // User requested automatic path. 
            // "Have a field to select the output directory" -> "Itstead of 'Output Folder' it should just be the current folder... + compiled"
            // This implies the Browse button might be redundant or should potentially just show the folder?
            // "The user requirement says 'Have a field to select the output directory', make sure it does NOT overwrite anything"
            // Actually, in the latest prompt: "Itstead of 'Output Folder' it should just be the current folder... + compiled... It fill just create the file there"
            // So we can keep the field read-only or just let them change the filename if they really want, but simpler to just auto-set it.
            // I will keep the browse button but make it open the target folder location instead of picking a new one?
            // Or just check if user wants to change it. 
            // Let's assume standard browse behavior but defaulting to the 'compiled' logic.
            
            var folder = _filePickerService.OpenFolder();
            if (folder != null && !string.IsNullOrWhiteSpace(ProjectPath))
            {
                var folderName = Path.GetFileName(ProjectPath);
                OutputApkPath = Path.Combine(folder, $"{folderName}.apk");
            }
        }

        [RelayCommand(CanExecute = nameof(CanRunBuild))]
        private async Task RunBuild()
        {
            if (string.IsNullOrWhiteSpace(ProjectPath))
            {
                 MessageBox.Show(Properties.Resources.SelectProjectFolder, Properties.Resources.BuildHeader, MessageBoxButton.OK, MessageBoxImage.Warning);
                 return;
            }

            var apktoolPath = _settingsService.Settings.ApktoolPath?.Trim();
             if (string.IsNullOrWhiteSpace(apktoolPath) || !File.Exists(apktoolPath))
            {
                MessageBox.Show(string.Format(Properties.Resources.Error_InvalidApktoolPath, apktoolPath), Properties.Resources.SettingsHeader, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (File.Exists(OutputApkPath))
            {
                 var result = MessageBox.Show($"The output file '{OutputApkPath}' already exists. Overwrite?", "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                 if (result != MessageBoxResult.Yes) return;
            }

            SetConsoleLog(Properties.Resources.StartingApktool);
            IsRunning = true;

            try
            {
                var exitCode = await _apktoolRunner.RunBuildAsync(ProjectPath, OutputApkPath, UseAapt2, ForceAll);

                if (exitCode == 0)
                {
                    AppendLog(Properties.Resources.BuildSuccessful);
                     MessageBox.Show(Properties.Resources.BuildSuccessful, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                     AppendLog($"{Properties.Resources.BuildFailed} (Exit Code: {exitCode})");
                     MessageBox.Show(Properties.Resources.BuildFailed, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"{Properties.Resources.BuildFailed}: {ex.Message}");
                MessageBox.Show(ex.Message, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
            return !IsRunning && !string.IsNullOrWhiteSpace(ProjectPath) && !string.IsNullOrWhiteSpace(OutputApkPath);
        }

        private string BuildCommandPreview()
        {
             var apktoolPath = _settingsService.Settings.ApktoolPath?.Trim();
            var apktool = string.IsNullOrWhiteSpace(apktoolPath) ? "<set apktool path>" : $"\"{apktoolPath}\"";
            var project = string.IsNullOrWhiteSpace(ProjectPath) ? "<select project>" : $"\"{ProjectPath}\"";
            var output = string.IsNullOrWhiteSpace(OutputApkPath) ? "<output apk>" : $"\"{OutputApkPath}\"";

            var builder = new StringBuilder();
            builder.Append($"java -jar {apktool} b {project} -o {output}");
            if(UseAapt2) builder.Append(" --use-aapt2");

            return $"Command preview: {builder}";
        }
    }
}
