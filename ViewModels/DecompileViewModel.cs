using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Windows;
using PulseAPK.Utils;

namespace PulseAPK.ViewModels
{
    public partial class DecompileViewModel : ObservableObject
    {
        private readonly ObservableCollection<string> _consoleLines = new ObservableCollection<string>();
        private readonly List<string> _pendingLogLines = new List<string>();
        private readonly object _logLock = new object();
        private readonly StringBuilder _fullLogBuilder = new StringBuilder();
        private bool _logFlushScheduled;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsHintVisible))]
        private string _apkPath;

        [ObservableProperty]
        private bool _decodeResources = true;

        [ObservableProperty]
        private bool _decodeSources = true;

        [ObservableProperty]
        private bool _keepOriginalManifest;

        [ObservableProperty]
        private string? _outputFolder;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunDecompileCommand))]
        private bool _isRunning;

        private bool _isConsolePreviewActive = true;

        private readonly Services.IFilePickerService _filePickerService;
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.ApktoolRunner _apktoolRunner;

        public bool IsHintVisible => string.IsNullOrEmpty(ApkPath);
        public ObservableCollection<string> ConsoleLines => _consoleLines;

        public DecompileViewModel()
        {
            _filePickerService = new Services.FilePickerService();
            _settingsService = new Services.SettingsService();
            _apktoolRunner = new Services.ApktoolRunner(_settingsService);

            _apktoolRunner.OutputDataReceived += OnOutputDataReceived;

            OutputFolder = Utils.PathUtils.GetDefaultDecompilePath();

            UpdateCommandPreview();

            RunDecompileCommand.NotifyCanExecuteChanged();
        }

        partial void OnApkPathChanged(string value)
        {
            UpdateCommandPreview();
            RunDecompileCommand.NotifyCanExecuteChanged();
        }

        partial void OnDecodeResourcesChanged(bool value) => UpdateCommandPreview();

        partial void OnDecodeSourcesChanged(bool value) => UpdateCommandPreview();

        partial void OnKeepOriginalManifestChanged(bool value) => UpdateCommandPreview();

        partial void OnOutputFolderChanged(string? value) => UpdateCommandPreview();

        [RelayCommand]
        private void BrowseApk()
        {
            var file = _filePickerService.OpenFile("APK Files (*.apk)|*.apk|All Files (*.*)|*.*");
            if (file != null)
            {
                var (isValid, message) = Utils.FileSanitizer.ValidateApk(file);
                if (!isValid)
                {
                    MessageBoxUtils.ShowError(message, "Invalid APK File");
                    return;
                }
                ApkPath = file;
            }
        }

        [RelayCommand]
        private void BrowseOutputFolder()
        {
            var folder = Utils.BrowseUtils.BrowseFolder(
                _filePickerService,
                OutputFolder,
                Properties.Resources.Error_OutputFolderNotSet,
                Properties.Resources.Error_FolderNotFound,
                requireExistingBasePath: true);

            if (folder != null)
            {
                OutputFolder = folder;
            }
        }

        [RelayCommand]
        private void OpenOutputFolder()
        {
            Utils.BrowseUtils.TryOpenFolder(OutputFolder, Properties.Resources.Error_OutputFolderNotSet, Properties.Resources.Error_FolderNotFound);
        }

        [RelayCommand(CanExecute = nameof(CanRunDecompile))]
        private async Task RunDecompile()
        {
            if (string.IsNullOrWhiteSpace(ApkPath))
            {
                MessageBoxUtils.ShowWarning("Please select an APK file to decompile.", "Missing APK");
                return;
            }

            var apktoolPath = _settingsService.Settings.ApktoolPath?.Trim();
            if (string.IsNullOrWhiteSpace(apktoolPath))
            {
                MessageBoxUtils.ShowWarning(Properties.Resources.Error_MissingApktool, Properties.Resources.SettingsHeader);
                return;
            }

            if (!File.Exists(apktoolPath))
            {
                MessageBoxUtils.ShowError(string.Format(Properties.Resources.Error_InvalidApktoolPath, apktoolPath), Properties.Resources.Error_InvalidApkFile);
                RunDecompileCommand.NotifyCanExecuteChanged();
                return;
            }

            SetConsoleLog(Properties.Resources.StartingApktool);

            var outputDir = !string.IsNullOrWhiteSpace(OutputFolder)
                ? OutputFolder
                : Path.Combine(Path.GetDirectoryName(ApkPath)!, Path.GetFileNameWithoutExtension(ApkPath));

            var normalizedOutputDir = Path.GetFullPath(outputDir);

            if (IsHighRiskOutputDirectory(normalizedOutputDir))
            {
                MessageBoxUtils.ShowError($"The selected output folder '{normalizedOutputDir}' is unsafe. Choose a different location.", "Invalid output folder");
                return;
            }

            var forceOverwrite = false;

            if (Directory.Exists(normalizedOutputDir))
            {
                var isEmpty = !Directory.EnumerateFileSystemEntries(normalizedOutputDir).Any();

                if (!isEmpty)
                {
                    var result = MessageBoxUtils.ShowQuestion($"The output directory '{normalizedOutputDir}' already exists and is not empty. Overwrite its contents?", "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                forceOverwrite = true;
            }

            IsRunning = true;

            try
            {
                var exitCode = await _apktoolRunner.RunDecompileAsync(ApkPath, normalizedOutputDir, DecodeResources, DecodeSources, KeepOriginalManifest, forceOverwrite);

                if (exitCode == 0)
                {
                    AppendLog(Properties.Resources.DecompileSuccessful);
                    MessageBoxUtils.ShowInfo(Properties.Resources.DecompileSuccessful);
                }
                else
                {
                    AppendLog($"{Properties.Resources.DecompileFailed} (Exit Code: {exitCode})");
                    MessageBoxUtils.ShowError(Properties.Resources.DecompileFailed);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"{Properties.Resources.DecompileFailed}: {ex.Message}");
                MessageBoxUtils.ShowError(ex.Message);
            }
            finally
            {
                IsRunning = false;
                RunDecompileCommand.NotifyCanExecuteChanged();
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
            if (!_isConsolePreviewActive)
            {
                return;
            }

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

        private bool CanRunDecompile()
        {
            return !IsRunning
                && !string.IsNullOrWhiteSpace(ApkPath)
                && !string.IsNullOrWhiteSpace(_settingsService.Settings.ApktoolPath);
        }

        private string BuildCommandPreview()
        {
            var apktoolPath = _settingsService.Settings.ApktoolPath?.Trim();
            var apktool = string.IsNullOrWhiteSpace(apktoolPath)
                ? "<set apktool path>"
                : $"\"{apktoolPath}\"";

            var apkInput = string.IsNullOrWhiteSpace(ApkPath)
                ? "<select apk>"
                : $"\"{ApkPath}\"";

            var outputDir = !string.IsNullOrWhiteSpace(OutputFolder)
                ? OutputFolder
                : !string.IsNullOrWhiteSpace(ApkPath)
                    ? Path.Combine(Path.GetDirectoryName(ApkPath)!, Path.GetFileNameWithoutExtension(ApkPath))
                    : "<output folder>";

            var builder = new StringBuilder();
            builder.Append($"java -jar {apktool} d {apkInput} -o \"{outputDir}\"");

            if (!DecodeResources) builder.Append(" -r");
            if (!DecodeSources) builder.Append(" -s");
            if (KeepOriginalManifest) builder.Append(" -m");

            return $"Command preview: {builder}";
        }

        private static bool IsHighRiskOutputDirectory(string outputDir)
        {
            var normalizedOutput = NormalizePath(outputDir);
            var outputRoot = Path.GetPathRoot(normalizedOutput);

            var riskyPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
            };

            if (!string.IsNullOrWhiteSpace(outputRoot) && string.Equals(normalizedOutput, NormalizePath(outputRoot), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return riskyPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(NormalizePath)
                .Any(riskyPath => IsSameOrSubPath(riskyPath, normalizedOutput));
        }

        private static bool IsSameOrSubPath(string basePath, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(candidatePath))
            {
                return false;
            }

            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (string.Equals(basePath, candidatePath, comparison))
            {
                return true;
            }

            if (basePath.Length == 1 && (basePath[0] == Path.DirectorySeparatorChar || basePath[0] == Path.AltDirectorySeparatorChar))
            {
                return false;
            }

            var basePathWithSeparator = basePath.EndsWith(Path.DirectorySeparatorChar)
                ? basePath
                : basePath + Path.DirectorySeparatorChar;

            return candidatePath.StartsWith(basePathWithSeparator, comparison);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var fullPath = Path.GetFullPath(path);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var root = Path.GetPathRoot(fullPath);

            if (!string.IsNullOrWhiteSpace(root) && string.Equals(fullPath, root, comparison))
            {
                return root.Length > 1
                    ? root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    : root;
            }

            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
