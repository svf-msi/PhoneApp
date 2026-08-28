using MicroVue.Views;
using System.Diagnostics;
using System.Globalization;

namespace MicroVue
{
    public partial class App : Application
    {
        #region Static section

        public static bool DevMode { get; private set; } = false;

        public static string SettingUsedBefore { get; protected set; } = "used_before";

        public static string SettingIsRegistered { get; protected set; } = "is_registered";

        public static string SettingEndDate { get; protected set; } = "end_date";

        public static string SettingIsValidated { get; protected set; } = "is_validated";

        public static int TrialPeriod { get; protected set; } = 365; // days

        public static string DataFolder { get; protected set; } = FileSystem.Current.AppDataDirectory + "/Data/";

        public static bool GetSecureSetting<T>(string setting, out T value) where T : IParsable<T>
        {
            value = default(T);
            try
            {
                var valueString = SecureStorage.Default.GetAsync(setting).Result;
                //Debug.WriteLine($"[Debug]: getting {setting} => {valueString}");
                if (T.TryParse(valueString, CultureInfo.InvariantCulture, out value)) return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in getting setting: {e}");
            }
            return false;
        }

        public static async Task<T> GetSecureSettingAsync<T> (string setting, T defaultValue = default(T)) where T : IParsable<T>
        {
            try
            {
                var defaultString = defaultValue?.ToString();
                var valueString = await GetSecureSettingStringAsync(setting, defaultString);
                //Debug.WriteLine($"[Debug]: getting {setting} => {valueString}");
                if (valueString != null && T.TryParse(valueString, CultureInfo.InvariantCulture, out T value)) return value;
                else return default(T);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in getting setting: {e}");
                return default(T);
            }
        }

        public static async Task<string?> GetSecureSettingStringAsync(string setting, string? defaultValue = null)
        {
            try
            {
                var value = await SecureStorage.Default.GetAsync(setting);

                if (value == null && defaultValue != null)
                {
                    await SecureStorage.Default.SetAsync(setting, defaultValue);
                    return defaultValue;
                }

                return value;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in getting secure setting {setting}: {e}");
                return null;
            }
        }

        public static async Task<bool?> GetSecureSettingBool(string setting, bool? defaultValue = null)
        {
            try
            {
                var defaultString = defaultValue != null ? defaultValue.ToString() : null;
                var valueString = await GetSecureSettingAsync(setting, defaultString);
                Debug.WriteLine($"[Debug]: getting {setting} => {valueString}");
                if (valueString != null && bool.TryParse(valueString, out bool boolValue)) return boolValue;
                else return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in getting setting: {e}");
                return null;
            }
        }

        public static void SetSecureSetting<T>(string setting, T value)
        {
            Task.Run(async () => await SetSecureSettingAsync<T>(setting, value));
        }

        public static async Task SetSecureSettingAsync<T>(string setting, T value)
        {
            if (value != null) await SetSecureSettingStringAsync(setting, value.ToString());
        }

        public static async Task SetSecureSettingStringAsync(string setting, string value)
        {
            try
            {
                await SecureStorage.Default.SetAsync(setting, value);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in setting secure setting {setting} to {value}: {e}");
            }
        }

        #endregion

        public App()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(AnalysisPage), typeof(AnalysisPage));
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            window.Created += (s, e) =>
            {
                DeviceDisplay.KeepScreenOn = true;
            };

            return window;
        }
    }
}