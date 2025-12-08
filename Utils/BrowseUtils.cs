using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PulseAPK.Utils
{
    public static class BrowseUtils
    {
        public static string? BrowseFolder(
            Services.IFilePickerService filePickerService,
            string? currentPath,
            string? missingPathMessage,
            string? folderNotFoundMessage,
            bool requireExistingBasePath)
        {
            if (filePickerService == null)
            {
                throw new ArgumentNullException(nameof(filePickerService));
            }

            var hasBaseDirectory = TryNormalizeDirectory(currentPath, out var normalizedBaseDirectory);

            if (requireExistingBasePath && !hasBaseDirectory)
            {
                ShowFolderError(currentPath, missingPathMessage, folderNotFoundMessage);
                return null;
            }

            var selectedFolder = filePickerService.OpenFolder(hasBaseDirectory ? normalizedBaseDirectory : null);

            if (!TryNormalizeDirectory(selectedFolder, out var normalizedSelection))
            {
                if (!string.IsNullOrWhiteSpace(selectedFolder))
                {
                    ShowFolderError(selectedFolder, missingPathMessage, folderNotFoundMessage);
                }
                return null;
            }

            return normalizedSelection;
        }

        public static bool TryOpenFolder(string? folderPath, string missingFolderMessage, string folderNotFoundMessage)
        {
            if (!TryNormalizeDirectory(folderPath, out var normalizedPath))
            {
                ShowFolderError(folderPath, missingFolderMessage, folderNotFoundMessage);
                return false;
            }

            try
            {
                Process.Start("explorer.exe", normalizedPath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Properties.Resources.Error_CouldNotOpenFolder, ex.Message), Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static bool TryNormalizeDirectory(string? path, out string normalizedPath)
        {
            normalizedPath = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                var fullPath = Path.GetFullPath(path.Trim());
                if (!Directory.Exists(fullPath))
                {
                    return false;
                }

                normalizedPath = fullPath;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ShowFolderError(string? path, string? missingPathMessage, string? folderNotFoundMessage)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                if (!string.IsNullOrWhiteSpace(missingPathMessage))
                {
                    MessageBox.Show(missingPathMessage, Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            if (!string.IsNullOrWhiteSpace(folderNotFoundMessage))
            {
                MessageBox.Show(string.Format(folderNotFoundMessage, path), Properties.Resources.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
