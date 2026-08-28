using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        string label = $"MicroVue™ version {AppInfo.Current.VersionString}";

        [ObservableProperty]
        bool isRegistered = AppShellViewModel.Instance.IsRegistered;

        [ObservableProperty]
        bool isValidated = AppShellViewModel.Instance.IsValidated;

        [ObservableProperty]
        bool isExpired = AppShellViewModel.Instance.IsExpired;

        [RelayCommand]
        void Register()
        {
            //IsValidated = true;
        }
    }
}
