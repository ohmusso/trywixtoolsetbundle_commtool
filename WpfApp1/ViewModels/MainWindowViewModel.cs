using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
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
        public partial bool IsInstall{ get; set; } = false;
        [ObservableProperty]
        public partial string DispText { get; set; } = "none";
        [ObservableProperty]
        public partial bool IsShortcut{ get; set; } = false;
        [ObservableProperty]
        public partial bool IsNotInstalling { get; set; } = true;

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
            IsNotInstalling = false;
            _ba.StartInstallation();
        }

        [RelayCommand]
        private void UnInstall()
        {
            IsNotInstalling = false;
            _ba.StartUninstallation();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (IsNotInstalling)
            {
                // 終了
                _ba.BADispatcher.InvokeShutdown();
                return;
            }
            // 終了処理をキャンセル
            e.Cancel = true;
        }
    }
}
