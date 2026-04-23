using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    using System.Windows;
    using WixToolset.BootstrapperApplicationApi;
    public partial class MainWindow : Window
    {
        private readonly CustomBA _ba;
        public MainWindow(CustomBA ba)
        {
            InitializeComponent();
            _ba = ba;
        }
        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. どのアクション（インストール/アンインストール等）を行うか計画する
            _ba.StartInstallation();
        }
        private void UnInstallButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. どのアクション（インストール/アンインストール等）を行うか計画する
            _ba.StartUninstallation();
        }
    }
}