using System.Windows;
using System.Windows.Controls;

namespace APKToolUI.Views
{
    public partial class DecompileView : UserControl
    {
        public DecompileView()
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
                if (files != null && files.Length > 0 && files[0].EndsWith(".apk", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (DataContext is ViewModels.DecompileViewModel vm)
                    {
                        vm.ApkPath = files[0];
                    }
                }
            }
        }
    }
}
