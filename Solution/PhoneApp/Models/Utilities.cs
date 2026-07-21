using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if ANDROID
using Android.Media;
#endif

#if IOS
using AVFoundation;
using CoreGraphics;
#endif

namespace MicroVue.Models
{
    public static class Utilities
    {
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
    }

}
