using System.Windows;
using System.Windows.Controls;
using PulseAPK.Utils;
using PulseAPK.ViewModels;

namespace PulseAPK.Views
{
    public partial class AnalyserView : UserControl
    {
        public AnalyserView()
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
                if (files.Length > 0 && DataContext is AnalyserViewModel viewModel)
                {
                    // For analyser, we expect a FOLDER with Smali files
                    string path = files[0];
                    if (System.IO.Directory.Exists(path))
                    {
                        // Check if folder contains .smali files
                        var smaliFiles = System.IO.Directory.GetFiles(path, "*.smali", System.IO.SearchOption.AllDirectories);
                        if (smaliFiles.Length > 0)
                        {
                            viewModel.ProjectPath = path;
                        }
                        else
                        {
                            MessageBoxUtils.ShowWarning(Properties.Resources.Error_InvalidSmaliProject, Properties.Resources.AnalyserHeader);
                        }
                    }
                    else
                    {
                        MessageBoxUtils.ShowWarning(Properties.Resources.Error_InvalidProjectSelection, Properties.Resources.AnalyserHeader);
                    }
                }
            }
        }
    }
}
