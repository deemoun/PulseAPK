using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PulseAPK.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void LinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string uri)
            {
                Process.Start(new ProcessStartInfo(uri)
                {
                    UseShellExecute = true
                });
            }
        }
    }
}
