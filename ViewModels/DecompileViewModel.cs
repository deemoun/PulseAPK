using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Windows;
using System.Diagnostics;

namespace PulseAPK.ViewModels
{
    public partial class DecompileViewModel : ObservableObject
    {
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
        private string _consoleLog = Properties.Resources.WaitingForCommand;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunDecompileCommand))]
        private bool _isRunning;

        private bool _isConsolePreviewActive = true;

        private readonly Services.IFilePickerService _filePickerService;
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.ApktoolRunner _apktoolRunner;

        public bool IsHintVisible => string.IsNullOrEmpty(ApkPath);

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
                    MessageBox.Show(message, "Invalid APK File", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                ApkPath = file;
            }
        }

        [RelayCommand]
        private void BrowseOutputFolder()
        {
            var folder = _filePickerService.OpenFolder(OutputFolder);
            if (folder != null)
            {
                OutputFolder = folder;
            }
        }

        [RelayCommand]
        private void OpenOutputFolder()
        {
            if (string.IsNullOrWhiteSpace(OutputFolder))
            {
                MessageBox.Show(Properties.Resources.Error_OutputFolderNotSet, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(OutputFolder))
            {
                MessageBox.Show(string.Format(Properties.Resources.Error_FolderNotFound, OutputFolder), Properties.Resources.Error_OutputFolderNotSet, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start("explorer.exe", OutputFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Properties.Resources.Error_CouldNotOpenFolder, ex.Message), Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanRunDecompile))]
        private async Task RunDecompile()
        {
            if (string.IsNullOrWhiteSpace(ApkPath))
            {
                MessageBox.Show("Please select an APK file to decompile.", "Missing APK", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var apktoolPath = _settingsService.Settings.ApktoolPath?.Trim();
            if (string.IsNullOrWhiteSpace(apktoolPath))
            {
                MessageBox.Show(Properties.Resources.Error_MissingApktool, Properties.Resources.SettingsHeader, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(apktoolPath))
            {
                MessageBox.Show(string.Format(Properties.Resources.Error_InvalidApktoolPath, apktoolPath), Properties.Resources.Error_InvalidApkFile, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"The selected output folder '{normalizedOutputDir}' is unsafe. Choose a different location.", "Invalid output folder", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var forceOverwrite = false;

            if (Directory.Exists(normalizedOutputDir))
            {
                var isEmpty = !Directory.EnumerateFileSystemEntries(normalizedOutputDir).Any();

                if (!isEmpty)
                {
                    var result = MessageBox.Show($"The output directory '{normalizedOutputDir}' already exists and is not empty. Overwrite its contents?", "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

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
                    MessageBox.Show(Properties.Resources.DecompileSuccessful, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AppendLog($"{Properties.Resources.DecompileFailed} (Exit Code: {exitCode})");
                    MessageBox.Show(Properties.Resources.DecompileFailed, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"{Properties.Resources.DecompileFailed}: {ex.Message}");
                MessageBox.Show(ex.Message, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (!_isConsolePreviewActive)
            {
                return;
            }

            ConsoleLog = BuildCommandPreview();
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
