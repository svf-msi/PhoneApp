using CommunityToolkit.Maui;
using Emgu.CV;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using MicroVue.ViewModels;
using MicroVue.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace MicroVue
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    System.Diagnostics.Debug.WriteLine($"UNHANDLED: {e.ExceptionObject}");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"UNOBSERVED: {e.Exception}");
                e.SetObserved();
            };

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
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler<MicroVue.Views.CameraPreview, MicroVue.Handlers.CameraPreviewHandler>();
                })
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

            builder.Services.AddSingleton<ScenesPage>();
            builder.Services.AddSingleton<ScenesViewModel>();
            builder.Services.AddSingleton<CameraPage>();
            builder.Services.AddSingleton<CameraViewModel>();
            builder.Services.AddSingleton<ImportPage>();
            builder.Services.AddSingleton<ImportViewModel>();
            builder.Services.AddTransient<AnalysisPage>();
            builder.Services.AddTransient<AnalysisViewModel>();

            return builder.Build();
        }
    }
}