using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.ViewModels
{
    public partial class AppShellViewModel : ObservableObject
    {
        public static AppShellViewModel Instance { get; protected set; } = new AppShellViewModel();

        [ObservableProperty]
        public bool isValidated = true;

        protected AppShellViewModel() 
        {
            
        }
    }
}
