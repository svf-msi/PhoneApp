using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
                Debug.WriteLine($"[Debug]: test {fps}");
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
    }

    public enum MetaType { FrameRate, FrameCount, VideoWidth, VideoHeight, VideoRotation, Duration }
}
