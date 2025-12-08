using System.Windows;
using System.Windows.Controls;
using PulseAPK.ViewModels;

namespace PulseAPK.Views
{
    public partial class BuildView : UserControl
    {
        public BuildView()
        {
            InitializeComponent();
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && DataContext is BuildViewModel viewModel)
                {
                    // For build, we expect a FOLDER
                    string path = files[0];
                    if (System.IO.Directory.Exists(path))
                    {
                        // Validate it's a project folder
                        var (isValid, message) = Utils.FileSanitizer.ValidateProjectFolder(path);
                        if (isValid)
                        {
                            viewModel.ProjectPath = path;
                        }
                        else
                        {
                             MessageBox.Show(message, Properties.Resources.Warning_InvalidProjectFolder, MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
        }
    }
}
