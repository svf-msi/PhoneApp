using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

#if ANDROID
using Android.Media;
using SkiaSharp;
#endif

#if IOS
using AVFoundation;
using CoreGraphics;
#endif

namespace VibSense.Models
{
    public static class Utilities
    {
        public static SkiaSharp.SKColor ConvertToSKColor(string colorText)
        {
            switch (colorText)
            {
                case "Red":
                    return SkiaSharp.SKColors.Red;
                case "Green":
                    return SkiaSharp.SKColors.Green;
                case "Blue":
                    return SkiaSharp.SKColors.Blue;
                case "Yellow":
                    return SkiaSharp.SKColors.Yellow;
                case "Teal":
                    return SkiaSharp.SKColors.Teal;
                case "Purple":
                    return SkiaSharp.SKColors.Purple;
                default:
                    return SkiaSharp.SKColors.Black;

            }
        }

        public static string ListFolderContents(string path)
        {
            var builder = new StringBuilder();
            if (Directory.Exists(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                FileInfo[] files = directory.GetFiles();

                builder.AppendLine($"{"File Name",-40} | {"Size (Bytes)",-15}");
                builder.AppendLine(new string('-', 60));

                foreach (FileInfo file in files)
                {
                    builder.AppendLine($"{file.Name,-40} | {file.Length,-15:N0}");
                }
                return builder.ToString();
            }
            else
            {
                Debug.WriteLine("The specified folder path does not exist.");
                return "";
            }
        }

        public static string GetHardwareId()
        {
            try
            {
                var deviceId = GetDeviceId();
                if (string.IsNullOrWhiteSpace(deviceId)) return "Failed to identify device ID";
                else return Hash(deviceId);
            }
            catch (Exception e)
            {
                return "Failed to identify hardware ID";
            }
        }

        public static string GetDeviceId()
        {
#if ANDROID
            // Returns the 64-bit Android ID (Settings.Secure.ANDROID_ID)
            var context = Android.App.Application.Context;
            return Android.Provider.Settings.Secure.GetString(context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);

#elif IOS
    // Returns the alphanumeric string unique to the device and vendor
    return UIKit.UIDevice.CurrentDevice.IdentifierForVendor?.ToString() ?? string.Empty;
    
#elif WINDOWS
    // Returns a unique hardware-based system ID for the publisher
    var systemId = Microsoft.System.GetSystemIdForPublisher();
    return Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(systemId.Id);
    
#else
            return string.Empty;
#endif
        }

        static string Hash(string input)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (var b in hash) // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));

                return sb.ToString();
            }
        }

        public static string GetDownloadsPath()
        {
            string path = string.Empty;

#if WINDOWS
    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
#elif ANDROID
            path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
#elif MACCATALYST
    var urls = Foundation.NSFileManager.DefaultManager.GetUrls(Foundation.NSSearchPathDirectory.DownloadsDirectory, Foundation.NSSearchPathDomain.User);
    path = urls[0].Path;
#endif

            return path;
        }
    }
}
