using MicroVue.Views;
using System.Diagnostics;

namespace MicroVue
{
    public partial class App : Application
    {
        public static string VideoFolder { get; set; } = "";

        public static string DataFolder { get; set; } = "";

        public static string FoiDataFolder { get; set; } = "";

        public static string FoiVideoFolder { get; set; } = "";

        public App()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(AnalysisPage), typeof(AnalysisPage));

            VideoFolder = FileSystem.Current.AppDataDirectory + "/Videos/";
            DataFolder = FileSystem.Current.AppDataDirectory + "/Data/";
            FoiDataFolder = FileSystem.Current.AppDataDirectory + "/FoiData/";
            FoiVideoFolder = FileSystem.Current.AppDataDirectory + "/FoiVideo/";

            if (!File.Exists(VideoFolder)) Directory.CreateDirectory(VideoFolder);
            if (!File.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            if (!File.Exists(FoiDataFolder)) Directory.CreateDirectory(FoiDataFolder);
            if (!File.Exists(FoiVideoFolder)) Directory.CreateDirectory(FoiVideoFolder);
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