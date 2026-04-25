using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using WpfApp1;

namespace TryWpfAppCommTool.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly CustomBA _ba;

        public MainWindowViewModel(CustomBA ba)
        {
            _ba = ba;
        }

        [ObservableProperty]
        public partial string DispText { get; set; } = "none";

        //[RelayCommand]
        //private void UpdateCount()
        //{
        //    Console.WriteLine("click");
        //    Count++;
        //    _countModel.count = Count;
        //}
        [RelayCommand]
        private void Install()
        {
            _ba.StartInstallation();
        }

        [RelayCommand]
        private void UnInstall()
        {
            _ba.StartUninstallation();
        }

    }
}
