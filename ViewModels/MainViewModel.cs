using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace APKToolUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        [ObservableProperty]
        private string _windowTitle = "APKTool UI";

        public MainViewModel()
        {
            // Default view
            CurrentView = new DecompileViewModel();
        }

        [RelayCommand]
        private void NavigateToDecompile()
        {
            CurrentView = new DecompileViewModel();
        }

        [RelayCommand]
        private void NavigateToBuild()
        {
            // CurrentView = new BuildViewModel();
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            // CurrentView = new SettingsViewModel();
        }
    }
}
