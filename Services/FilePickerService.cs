using Microsoft.Win32;
using System.Windows;

namespace APKToolUI.Services
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

        public string? OpenFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }
            return null;
        }
    }
}
