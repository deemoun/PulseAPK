using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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

        [ObservableProperty]
        private bool _keepOriginalManifest;

        [ObservableProperty]
        private string? _outputFolder;

        [ObservableProperty]
        private string _consoleLog = "Waiting for command...";

        private readonly Services.IFilePickerService _filePickerService;
        private readonly Services.ISettingsService _settingsService;
        private readonly Services.ApktoolRunner _apktoolRunner;

        public DecompileViewModel()
        {
            _filePickerService = new Services.FilePickerService();
            _settingsService = new Services.SettingsService();
            _apktoolRunner = new Services.ApktoolRunner(_settingsService);

            _apktoolRunner.OutputDataReceived += OnOutputDataReceived;
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
        private void BrowseOutputFolder()
        {
            var folder = _filePickerService.OpenFolder();
            if (folder != null)
            {
                OutputFolder = folder;
            }
        }

        [RelayCommand]
        private async Task RunDecompile()
        {
            if (string.IsNullOrWhiteSpace(ApkPath))
            {
                MessageBox.Show("Please select an APK file to decompile.", "Missing APK", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConsoleLog = "Starting apktool...";

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

                    forceOverwrite = true;
                }
            }

            try
            {
                var exitCode = await _apktoolRunner.RunDecompileAsync(ApkPath, normalizedOutputDir, DecodeResources, DecodeSources, KeepOriginalManifest, forceOverwrite);

                if (exitCode == 0)
                {
                    AppendLog("Decompile finished.");
                }
                else
                {
                    AppendLog($"Decompile failed with exit code {exitCode}. Check the log above for details.");
                    MessageBox.Show("Apktool reported an error while decompiling. Please review the console log for details.", "Decompile failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Decompile failed: {ex.Message}");
                MessageBox.Show(ex.Message, "Decompile failed", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (string.IsNullOrWhiteSpace(ConsoleLog) || ConsoleLog == "Waiting for command...")
            {
                ConsoleLog = message;
            }
            else
            {
                ConsoleLog += $"{Environment.NewLine}{message}";
            }
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
