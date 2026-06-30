using MicroVue.Views;
using System.Diagnostics;

namespace MicroVue
{
    public partial class App : Application
    {
        public static string VideoFolder { get; set; } = "";

        public static string DataFolder { get; set; } = "";

        public App()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(AnalysisPage), typeof(AnalysisPage));

            VideoFolder = FileSystem.Current.AppDataDirectory + "/Videos/";
            DataFolder = FileSystem.Current.AppDataDirectory + "/Data/";

            if (!File.Exists(VideoFolder)) Directory.CreateDirectory(VideoFolder);
            if (!File.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}