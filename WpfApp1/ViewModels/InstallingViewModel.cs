using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows;
using WpfApp1;

namespace WpfApp1.ViewModels
{
    public partial class InstallingViewModel : ObservableObject
    {
        private readonly CustomBA _ba;

        public InstallingViewModel(CustomBA ba)
        {
            _ba = ba;
        }

    }
}
