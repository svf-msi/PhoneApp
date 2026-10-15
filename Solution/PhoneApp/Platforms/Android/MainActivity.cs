using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Java.Lang;
using System.Reflection;
using static Android.Graphics.ImageDecoder;

namespace MicroVue
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"[Debug]: {"AndroidError"} - {args?.Exception}");
                if (args?.Exception == null) return;

                string downloadsPath = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
                string filePath = Path.Combine(downloadsPath, "AndroidError.txt");
                var error = args.Exception.ToString();
                if (args.Exception is TargetInvocationException ex && ex.InnerException != null)
                {
                    error += "/n" + ex.InnerException.ToString();
                }
                File.WriteAllText(filePath, error);
            };

            Java.Lang.Thread.DefaultUncaughtExceptionHandler = new MyJavaUncaughtExceptionHandler();
        }
    }

    public class MyJavaUncaughtExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        public void UncaughtException(Java.Lang.Thread thread, Throwable ex)
        {
            // Log native/Java exception details here
            System.Diagnostics.Debug.WriteLine($"[Debug]: Java Thread Crash - {thread.Name}, Error: {ex.Message}");

            string downloadsPath = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
            string filePath = Path.Combine(downloadsPath, "JavaThreadError.txt");
            var error = ex.ToString();
            File.WriteAllText(filePath, error);
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
        }
    }
}
