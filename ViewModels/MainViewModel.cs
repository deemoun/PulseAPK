using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace PulseAPK.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        [ObservableProperty]
        private string _windowTitle = Properties.Resources.AppTitle;

        [ObservableProperty]
        private string _selectedMenu = "Decompile";

        public MainViewModel()
        {
            // Start on the decompile view.
            CurrentView = new DecompileViewModel();
        }

        [RelayCommand]
        private void NavigateToDecompile()
        {
            CurrentView = new DecompileViewModel();
            SelectedMenu = "Decompile";
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            CurrentView = new SettingsViewModel();
            SelectedMenu = "Settings";
        }

        [RelayCommand]
        private void NavigateToBuild()
        {
            CurrentView = new BuildViewModel();
            SelectedMenu = "Build";
        }

        [RelayCommand]
        private void NavigateToAnalyser()
        {
            CurrentView = new AnalyserViewModel();
            SelectedMenu = "Analyser";
        }

        [RelayCommand]
        private void NavigateToAbout()
        {
            CurrentView = new AboutViewModel();
            SelectedMenu = "About";
        }
    }
}
