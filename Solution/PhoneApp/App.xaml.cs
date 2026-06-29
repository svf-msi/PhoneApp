using MicroVue.Views;

namespace MicroVue
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(AnalysisPage), typeof(AnalysisPage));
            Routing.RegisterRoute(nameof(ImportPage), typeof(ImportPage));
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}