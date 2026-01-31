using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Reflection;

namespace PulseAPK.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string AppVersion => getAppVersion();
        public string DeveloperName { get; } = "Dmitry Yarygin";
        public string Year { get; } = "2026";

        private string getAppVersion()
        {
            var informationalVersion = Assembly.GetExecutingAssembly()
                                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                               .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var metadataSeparatorIndex = informationalVersion.IndexOf('+');
                if (metadataSeparatorIndex >= 0)
                {
                    informationalVersion = informationalVersion[..metadataSeparatorIndex];
                }

                var prereleaseSeparatorIndex = informationalVersion.IndexOf('-');
                if (prereleaseSeparatorIndex >= 0)
                {
                    informationalVersion = informationalVersion[..prereleaseSeparatorIndex];
                }
            }

            var version = string.IsNullOrWhiteSpace(informationalVersion)
                ? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.7"
                : informationalVersion;

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
                // Ignore failures to launch the browser.
            }
        }
    }
}


