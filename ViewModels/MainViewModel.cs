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

        [ObservableProperty]
        private string _selectedMenu = "Decompile";

        public MainViewModel()
        {
            // Default view
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
    }
}
