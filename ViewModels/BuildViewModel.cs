using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PulseAPK.Services;
using PulseAPK.Utils;

namespace PulseAPK.ViewModels
{
    public partial class BuildViewModel : ObservableObject
    {
        private readonly ObservableCollection<string> _consoleLines = new ObservableCollection<string>();
        private readonly List<string> _pendingLogLines = new List<string>();
        private readonly object _logLock = new object();
        private readonly StringBuilder _fullLogBuilder = new StringBuilder();
        private bool _logFlushScheduled;

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
        private bool _signApk = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunBuildCommand))]
        private bool _isRunning;

        private bool _isConsolePreviewActive = true;

        private readonly Services.IFilePickerService _filePickerService;
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.ApktoolRunner _apktoolRunner;
        private readonly Services.UbersignRunner _ubersignRunner;

        public bool IsHintVisible => string.IsNullOrEmpty(ProjectPath);

        public bool HasProject => !string.IsNullOrWhiteSpace(ProjectPath);
        public ObservableCollection<string> ConsoleLines => _consoleLines;

        public BuildViewModel()
        {
            _filePickerService = new Services.FilePickerService();
            _settingsService = new Services.SettingsService();
            _apktoolRunner = new Services.ApktoolRunner(_settingsService);
            _ubersignRunner = new Services.UbersignRunner(_settingsService);

            InitializeOutputPath();

            _apktoolRunner.OutputDataReceived += OnOutputDataReceived;
            _ubersignRunner.OutputDataReceived += OnOutputDataReceived;

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
        partial void OnSignApkChanged(bool value) => UpdateCommandPreview();

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

            var signedApkPath = SignApk ? GetSignedApkPath(OutputApkPath) : string.Empty;
            if (SignApk && !string.IsNullOrWhiteSpace(signedApkPath) && File.Exists(signedApkPath))
            {
                var result = MessageBoxUtils.ShowQuestion($"The signed output file '{signedApkPath}' already exists. Overwrite?", "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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

                    if (SignApk && !string.IsNullOrWhiteSpace(signedApkPath))
                    {
                        await RunSigningAsync(OutputApkPath, signedApkPath);
                    }

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
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => AppendLogInternal(message)));
                return;
            }

            AppendLogInternal(message);
        }

        private void AppendLogInternal(string message)
        {
            lock (_logLock)
            {
                if (_isConsolePreviewActive)
                {
                    _consoleLines.Clear();
                    _pendingLogLines.Clear();
                    _fullLogBuilder.Clear();
                    _isConsolePreviewActive = false;
                }

                var sanitized = message ?? string.Empty;
                AppendToFullLog(sanitized);
                _pendingLogLines.Add(sanitized);
                ScheduleLogFlush();
            }
        }

        private void SetConsoleLog(string message, bool isPreview = false)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => SetConsoleLog(message, isPreview)));
                return;
            }

            lock (_logLock)
            {
                var lines = SplitLines(message);
                _pendingLogLines.Clear();
                _fullLogBuilder.Clear();
                _isConsolePreviewActive = isPreview;

                _consoleLines.Clear();
                foreach (var line in lines)
                {
                    _consoleLines.Add(line);
                    AppendToFullLog(line);
                }

                _logFlushScheduled = false;
            }
        }

        private void UpdateCommandPreview()
        {
            if (!_isConsolePreviewActive) return;
            SetConsoleLog(BuildCommandPreview(), isPreview: true);
        }

        private void ScheduleLogFlush()
        {
            if (_logFlushScheduled)
            {
                return;
            }

            _logFlushScheduled = true;
            if (Application.Current == null)
            {
                FlushLog();
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(FlushLog));
        }

        private void FlushLog()
        {
            List<string> pendingLines;
            lock (_logLock)
            {
                pendingLines = _pendingLogLines.Count == 0 ? null : new List<string>(_pendingLogLines);
                _pendingLogLines.Clear();
                _logFlushScheduled = false;
            }

            if (pendingLines == null)
            {
                return;
            }

            foreach (var line in pendingLines)
            {
                _consoleLines.Add(line);
            }
        }

        private void AppendToFullLog(string message)
        {
            if (_fullLogBuilder.Length > 0)
            {
                _fullLogBuilder.AppendLine();
            }

            _fullLogBuilder.Append(message);
        }

        private static List<string> SplitLines(string message)
        {
            var normalized = (message ?? string.Empty).Replace("\r\n", "\n");
            return new List<string>(normalized.Split('\n'));
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

            var commandPreview = new StringBuilder($"Command preview: {builder}");

            var signingCommandPreview = BuildSigningCommandPreview(OutputApkPath);
            if (!string.IsNullOrWhiteSpace(signingCommandPreview))
            {
                commandPreview.Append($"{Environment.NewLine}{signingCommandPreview}");
            }

            return commandPreview.ToString();
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

        private async Task RunSigningAsync(string inputApk, string signedApkPath)
        {
            AppendLog($"Signing APK via ubersign to '{signedApkPath}'...");

            try
            {
                var exitCode = await _ubersignRunner.RunSigningAsync(inputApk, signedApkPath);

                if (exitCode == 0)
                {
                    AppendLog($"Signed APK created at '{signedApkPath}'.");
                }
                else
                {
                    AppendLog($"Signing failed (Exit Code: {exitCode}).");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Signing failed: {ex.Message}");
                MessageBoxUtils.ShowWarning(ex.Message, "Signing failed");
            }
        }

        private string GetSignedApkPath(string outputApkPath)
        {
            if (string.IsNullOrWhiteSpace(outputApkPath))
            {
                return string.Empty;
            }

            var folder = Path.GetDirectoryName(outputApkPath);
            if (string.IsNullOrWhiteSpace(folder))
            {
                return string.Empty;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputApkPath);
            var extension = Path.GetExtension(outputApkPath);

            return Path.Combine(folder, $"{fileNameWithoutExtension}_signed{extension}");
        }

        private string BuildSigningCommandPreview(string outputApk)
        {
            if (!SignApk)
            {
                return string.Empty;
            }

            var hasOutputPath = !string.IsNullOrWhiteSpace(outputApk) && outputApk != "<output apk>";
            var sanitizedOutputApk = hasOutputPath ? outputApk.Trim().Trim('"') : string.Empty;
            var hasExtension = hasOutputPath && !string.IsNullOrWhiteSpace(Path.GetExtension(sanitizedOutputApk));

            var signedApk = hasExtension ? GetSignedApkPath(sanitizedOutputApk) : string.Empty;

            string signingCommand;

            var appRoot = GetApplicationRootPath();
            var configuredUbersign = _settingsService.Settings.UbersignPath?.Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(configuredUbersign))
            {
                var resolvedUbersign = Path.IsPathRooted(configuredUbersign)
                    ? configuredUbersign
                    : Path.Combine(appRoot, configuredUbersign);

                var configuredExists = File.Exists(resolvedUbersign);
                var isJar = string.Equals(Path.GetExtension(resolvedUbersign), ".jar", StringComparison.OrdinalIgnoreCase);

                signingCommand = isJar
                    ? $"java -jar \"{resolvedUbersign}\""
                    : $"\"{resolvedUbersign}\"";

                if (!configuredExists)
                {
                    signingCommand = $"<{signingCommand} (not found)>";
                }
            }
            else
            {
                var ubersignJarPath = Path.Combine(appRoot, "ubersign.jar");
                var ubersignPath = Path.Combine(appRoot, "ubersign");
                var windowsUbersign = $"{ubersignPath}.exe";

                if (File.Exists(ubersignJarPath))
                {
                    signingCommand = $"java -jar \"{ubersignJarPath}\"";
                }
                else if (File.Exists(ubersignPath))
                {
                    signingCommand = $"\"{ubersignPath}\"";
                }
                else if (File.Exists(windowsUbersign))
                {
                    signingCommand = $"\"{windowsUbersign}\"";
                }
                else
                {
                    signingCommand = "<ubersign.jar in app root>";
                }
            }

            if (!hasOutputPath)
            {
                return "Signing preview: ubersign -a <output apk> -o <output folder>";
            }

            var sanitizedSignedApk = signedApk.Trim().Trim('"');

            var outputFolder = hasExtension
                ? Path.GetDirectoryName(sanitizedSignedApk) ?? string.Empty
                : sanitizedOutputApk;

            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = EnsureCompiledDirectory();
            }

            return $"Signing preview: {signingCommand} -a \"{sanitizedOutputApk}\" -o \"{outputFolder}\"";
        }
    }
}
