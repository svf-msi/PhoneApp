using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MicroVue.ViewModels;
using MicroVue.Views;
using Syncfusion.Maui.Toolkit.Hosting;

namespace MicroVue
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<ScenesPage>();
            builder.Services.AddSingleton<ScenesViewModel>();
            builder.Services.AddSingleton<ImportPage>();
            builder.Services.AddSingleton<ImportViewModel>();
            builder.Services.AddTransient<AnalysisPage>();
            builder.Services.AddTransient<AnalysisViewModel>();

            return builder.Build();
        }
    }
}