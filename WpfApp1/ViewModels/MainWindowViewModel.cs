using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows;
using WpfApp1;

namespace WpfApp1.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly CustomBA _ba;
        public StartViewModel startViewModel;
        public InstallingViewModel installingViewMoel;

        [ObservableProperty]
        private object? _currentViewModel;

        public MainWindowViewModel(CustomBA ba)
        {
            _ba = ba;
            startViewModel = new(ba);
            installingViewMoel = new(ba);

            _currentViewModel = startViewModel;
        }

        public void ShowInstallingView()
        {
            CurrentViewModel = installingViewMoel;
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!_ba.isInstalling)
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
