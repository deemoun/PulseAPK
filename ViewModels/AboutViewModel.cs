using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Reflection;

namespace APKToolUI.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string AppVersion => getAppVersion();
        public string DeveloperName { get; } = "Dmitry Yarygin";
        public string Year { get; } = "2025";
        
        private string getAppVersion() {
             var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
             return string.Format(Properties.Resources.About_Version, version);
        }

        [RelayCommand]
        private void OpenProjectPage()
        {
            OpenUrl("https://github.com/deemoun/PulseAPK");
        }

        [RelayCommand]
        private void OpenDeveloperPage()
        {
            OpenUrl("https://yarygintech.com/");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (System.Exception)
            {
                // Handle or ignore error if browser fails to open
            }
        }
    }
}
