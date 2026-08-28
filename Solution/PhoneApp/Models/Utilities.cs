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

namespace MicroVue.Models
{
    public static class Utilities
    {
        public static MetaType[] MetaTypes = new MetaType[] { MetaType.FrameRate, MetaType.FrameCount, MetaType.VideoWidth, MetaType.VideoHeight, MetaType.VideoRotation, MetaType.Duration };
        
        public static void GetMetadata(out Dictionary<MetaType, double> data, string file, IEnumerable<MetaType> parameters = null)
        {
            data = new Dictionary<MetaType, double>();
            if (parameters == null) parameters = MetaTypes;

#if ANDROID
            using (var retriever = new MediaMetadataRetriever())
            {
                retriever.SetDataSource(file);
                foreach (var parameter in parameters)
                {
                    try
                    {
                        data[parameter] = GetAndoidParameter(retriever, parameter);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Error in reading metadata: {e}");
                    }
                }
            }
#endif
        }

#if ANDROID
        static double GetAndoidParameter(MediaMetadataRetriever retriever, MetaType parameter)
        {
            double value = 0;
            var key = parameter switch
            {
                MetaType.FrameRate => MetadataKey.CaptureFramerate,
                MetaType.FrameCount => MetadataKey.VideoFrameCount,
                MetaType.VideoWidth => MetadataKey.VideoWidth,
                MetaType.VideoHeight => MetadataKey.VideoHeight,
                MetaType.VideoRotation => MetadataKey.VideoRotation,
                MetaType.Duration => MetadataKey.Duration,
            };

            var result = retriever.ExtractMetadata(key);
            double.TryParse(result, out value);
            return value;
        }
#endif

        public static int GetMp4Rotation(string filePath)
        {
            int rotationAngle = 0;
#if ANDROID
            using (var retriever = new MediaMetadataRetriever())
            {
                retriever.SetDataSource(filePath);
                string rotation = retriever.ExtractMetadata(MetadataKey.VideoRotation);
                if (int.TryParse(rotation, out int angle))
                {
                    rotationAngle = angle;
                }
                string fps = retriever.ExtractMetadata(MetadataKey.VideoFrameCount);
                //Debug.WriteLine($"[Debug]: test {fps}");
            }
#endif

#if IOS
    var asset = AVAsset.FromUrl(new NSUrl(filePath, false));
    var videoTrack = asset.TracksWithMediaType(AVMediaType.Video);
    if (videoTrack.Length > 0)
    {
        var transform = videoTrack[0].PreferredTransform;
        
        // Calculate the angle based on the transform matrix
        if (transform.a == 0 && transform.b == 1.0 && transform.c == -1.0 && transform.d == 0)
            rotationAngle = 90;
        else if (transform.a == -1.0 && transform.b == 0 && transform.c == 0 && transform.d == -1.0)
            rotationAngle = 180;
        else if (transform.a == 0 && transform.b == -1.0 && transform.c == 1.0 && transform.d == 0)
            rotationAngle = 270;
    }
#endif
            return rotationAngle;
        }

        public static int GetMp4FrameCount(string filePath)
        {
            int frameCount = 0;
#if ANDROID
            using (var retriever = new MediaMetadataRetriever())
            {
                retriever.SetDataSource(filePath);
                string length = retriever.ExtractMetadata(MetadataKey.VideoFrameCount);
                if (int.TryParse(length, out int count))
                {
                    frameCount = count;
                }
            }
#endif

#if IOS
    var asset = AVAsset.FromUrl(new NSUrl(filePath, false));
    var videoTrack = asset.TracksWithMediaType(AVMediaType.Video);
    if (videoTrack.Length > 0)
    {
        var transform = videoTrack[0].PreferredTransform;
        
        // Calculate the angle based on the transform matrix
        if (transform.a == 0 && transform.b == 1.0 && transform.c == -1.0 && transform.d == 0)
            frameCount = 90;
        else if (transform.a == -1.0 && transform.b == 0 && transform.c == 0 && transform.d == -1.0)
            frameCount = 180;
        else if (transform.a == 0 && transform.b == -1.0 && transform.c == 1.0 && transform.d == 0)
            frameCount = 270;
    }
#endif
            return frameCount;
        }

        public static Point Rotate(this Point point, int rotation, int width, int height)
        {
            switch (rotation)
            {
                case 1:
                case -3:
                case 90:
                case -270:
                    return new Point(height - point.Y, point.X);
                case 2:
                case -2:
                case 180:
                case -180:
                    return new Point(width - point.X, height - point.Y);
                case 3:
                case -1:
                case 270:
                case -90:
                    return new Point(point.Y, width - point.X);
                default: return point;
            }
        }

        public static Rect Rotate(this Rect rect, int rotation, int width, int height)
        {
            var point = Rotate(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), rotation, width, height);
            switch (rotation)
            {
                case 1:
                case -1:
                case 3:
                case -3:
                case 90:
                case -90:
                case 270:
                case -270:
                    return new Rect(point.X - rect.Height / 2, point.Y - rect.Width / 2, rect.Height, rect.Width);
                default: 
                    return new Rect(point.X - rect.Width / 2, point.Y - rect.Height / 2, rect.Width, rect.Height);
            }
        }

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
    }

    public enum MetaType { FrameRate, FrameCount, VideoWidth, VideoHeight, VideoRotation, Duration }
}
