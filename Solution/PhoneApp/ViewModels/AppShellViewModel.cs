using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.ViewModels
{
    public partial class AppShellViewModel : ObservableObject
    {
        public static AppShellViewModel Instance { get; protected set; } = new AppShellViewModel();

        [ObservableProperty]
        public bool isValidated = false;

        [ObservableProperty]
        public bool isRegistered = false;

        [ObservableProperty]
        public bool isExpired = false;

        protected AppShellViewModel() 
        {
            if (!App.DevMode)
            {
                Validate();
            }
        }

        protected void Validate()
        {
            if (App.GetSecureSetting<bool>(App.SettingUsedBefore, out var usedBefore))
            {
                if (App.GetSecureSetting(App.SettingIsRegistered, out bool registrationStatus))
                {
                    IsRegistered = registrationStatus;
                }
                else
                {
                    IsRegistered = false;
                }

                if (App.GetSecureSetting(App.SettingEndDate, out DateTime endDate))
                {
                    var today = DateTime.Today;
                    IsValidated = today <= endDate;
                    IsExpired = today > endDate;
                }
                else
                {
                    IsValidated = false;
                }
            }
            else
            {
                var endDate = DateTime.Today.AddDays(App.TrialPeriod).Date;
                App.SetSecureSetting(App.SettingUsedBefore, true);
                App.SetSecureSetting(App.SettingIsRegistered, false);
                App.SetSecureSetting(App.SettingEndDate, endDate);
                IsValidated = true;
                IsExpired = false;
            }
        }
    }
}
