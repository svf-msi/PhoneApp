using CommunityToolkit.Maui;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;
using VibSense.ViewModels;
using VibSense.Views;

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
            CvInvoke.UseOpenCL = false;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<AboutPage>();
            builder.Services.AddSingleton<AboutViewModel>();

            return builder.Build();
        }
    }
}
