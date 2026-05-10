using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows;
using WpfApp1;

namespace WpfApp1.ViewModels
{
    public partial class StartViewModel : ObservableObject
    {
        private readonly CustomBA _ba;

        public StartViewModel(CustomBA ba)
        {
            _ba = ba;
        }

        [ObservableProperty]
        public partial bool IsInstall { get; set; } = false;
        [ObservableProperty]
        public partial string DispText { get; set; } = "none";
        [ObservableProperty]
        public partial bool IsShortcut { get; set; } = false;

        [RelayCommand]
        private void Install()
        {
            _ba.StartInstallation();
        }
    }
}
