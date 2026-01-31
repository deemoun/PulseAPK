using System.Windows;
using PulseAPK.Services;

namespace PulseAPK;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var settingsService = new SettingsService();
        LocalizationService.Instance.Initialize(settingsService);
        base.OnStartup(e);
    }
}
