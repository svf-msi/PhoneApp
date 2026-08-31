using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenseManager.Library;
using MicroVue.Models;
using Newtonsoft.Json;
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
        static LicenseKeys LicenseKeys = new LicenseKeys("Mechanical Solutions Inc, VibVue 2024", "", "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEeM3I7UzfRH7O7kae56UDRx+jNgZOdpGm6D9zsqXIaNQhstpLyr7X6/3GLOBbr2/Fjl3je07P9RFqVMfpv3VV+Q==");
        
        [ObservableProperty]
        string label = $"MicroVue™ version {AppInfo.Current.VersionString}";

        [ObservableProperty]
        bool isRegistered = AppShellViewModel.Instance.IsRegistered;

        [ObservableProperty]
        bool isValidated = AppShellViewModel.Instance.IsValidated;

        [ObservableProperty]
        bool isExpired = AppShellViewModel.Instance.IsExpired;

        [ObservableProperty]
        string deviceId = Utilities.GetHardwareId();

        [RelayCommand]
        async Task CopyId()
        {
            await Clipboard.Default.SetTextAsync(DeviceId);
        }

        [RelayCommand]
        async Task Register()
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.plain-text" } },
                        { DevicePlatform.Android, new[] { "text/plain" } },          
                        { DevicePlatform.WinUI, new[] { ".txt" } },
                        { DevicePlatform.macOS, new[] { "public.plain-text" } }
                    });

                PickOptions options = new()
                {
                    PickerTitle = "Select a license file",
                    FileTypes = customFileType
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    var file = result.FullPath;
                    var text = File.ReadAllText(file);
                    var license = CustomerLicense.From(text);
                    if (license != null)
                    {
                        if (Verify(license))
                        {
                            IsRegistered = AppShellViewModel.Instance.IsRegistered = true;
                            IsValidated = AppShellViewModel.Instance.IsValidated = true; 
                            IsExpired = AppShellViewModel.Instance.IsExpired = false;
                            App.SetSecureSetting(App.SettingIsRegistered, true);
                            App.SetSecureSetting(App.SettingEndDate, license.EndDate);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in license picker: {e}");
            }
        }

        bool Verify(CustomerLicense license)
        {
            if (license == null) return false;

            //Debug.WriteLine($"[Debug]: verifying license {JsonConvert.SerializeObject(license, Formatting.Indented)}");
            //Debug.WriteLine($"[Debug]: check key {license.IsValid(LicenseKeys.PublicKey)}");
            if (!license.IsValid(LicenseKeys.PublicKey)) return false;

            //Debug.WriteLine($"[Debug]: check id {license.HostId} = {DeviceId}");
            if (license.HostId != DeviceId) return false;

            //Debug.WriteLine($"[Debug]: check module {license.Module} = {ModuleType.MicroVue}");
            if (license.Module != ModuleType.MicroVue) return false;

            //Debug.WriteLine($"[Debug]: check version {IsVersionValid(license.Version)}");
            if (!IsVersionValid(license.Version)) return false;

            //Debug.WriteLine($"[Debug]: check exp {!IsLicenseExpired(license)}");
            if (IsLicenseExpired(license)) return false;

            return true;
        }

        bool IsVersionValid(string version)
        {
            if (!string.IsNullOrWhiteSpace(version))
            {
                var vmm = version.Split('.');
                if (vmm.Length > 1)
                {
                    if (int.TryParse(vmm[0], out var major) && int.TryParse(vmm[1], out var minor))
                    {
                        var current = AppInfo.Current.Version;
                        if (current.Major < major || (current.Major == major && current.Minor <= minor)) return true;
                    }
                }
            }
            return false;
        }

        bool IsLicenseExpired(CustomerLicense license)
        {
            if (!string.IsNullOrWhiteSpace(license?.EndDate) && DateTime.TryParse(license.EndDate, out var endDate))
            {
                var today = DateTime.Today;
                if (today > endDate) return true;
            }
            return false;
        }
    }
}
