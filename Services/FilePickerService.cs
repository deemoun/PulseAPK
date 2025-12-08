using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace PulseAPK.Services
{
    public class FilePickerService : IFilePickerService
    {
        public string? OpenFile(string filter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            return null;
        }

        public string? OpenFolder(string? initialDirectory = null)
        {
            var dialog = new OpenFolderDialog();

            if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }
            return null;
        }
    }
}
