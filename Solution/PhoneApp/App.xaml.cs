using MicroVue.Views;
using System.Diagnostics;

namespace MicroVue
{
    public partial class App : Application
    {
        public static string DataFolder { get; set; } = FileSystem.Current.AppDataDirectory + "/Data/";

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