using CommunityToolkit.Maui;
using Emgu.CV;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using VibSense.ViewModels;
using VibSense.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace VibSense
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
#if ANDROID
            // Initializes the native camera and backend for Android
            Emgu.CV.CvInvokeAndroid.Init();
#elif IOS
        // Initializes the native backend for iOS
        //Emgu.CV.Platform.Maui.MauiInvoke.Init();
#endif

            // Example: Disable OpenCL to prevent some rendering bugs
            CvInvoke.UseOpenCL = false;
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement()
                .ConfigureSyncfusionToolkit()
                .UseSkiaSharp()
                .UseLiveCharts()
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
            builder.Services.AddSingleton<ProjectsPage>();
            builder.Services.AddSingleton<ProjectsViewModel>();
            builder.Services.AddSingleton<LivePage>();
            builder.Services.AddSingleton<LiveViewModel>();
            builder.Services.AddTransient<AboutPage>();
            builder.Services.AddTransient<AboutViewModel>();

            return builder.Build();
        }
    }
}