using CommunityToolkit.Maui;
using Emgu.CV;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using MicroVue.ViewModels;
using MicroVue.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace MicroVue
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
                .UseMauiCommunityToolkitMediaElement(false)
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
            builder.Services.AddTransient<AboutPage>();
            builder.Services.AddTransient<AboutViewModel>();

            AppDomain.CurrentDomain.UnhandledException += (_, args) => LogError("AppDomainError", args?.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, args) => LogError("TaskSchedulerError", args?.Exception);

            return builder.Build();
        }

        static void LogError(string source, Exception e)
        {
            Debug.WriteLine($"[Debug]: {source} - {e}");
            if (e == null) return;

#if ANDROID
            string downloadsPath = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
            string filePath = Path.Combine(downloadsPath, source + ".txt");
            var error = e.ToString();
            if (e is TargetInvocationException ex && ex.InnerException != null) 
            {
                error += "/n" + ex.InnerException.ToString();
            }
            File.WriteAllText(filePath, error);
#endif
        }
    }
}