using Emgu.CV;
using Emgu.CV.Structure;
using MathNet.Numerics.LinearRegression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StandardLib
{
    public class TrackingAnalysis
    {
        public static (double x, double y) CalculateShift(Image<Gray, float> image, List<GradientPoint> points)
        {
            var size = points.Count;
            double[] differences = new double[size];
            double[][] gradients = new double[size][];
            double[] shift = new double[] { 0, 0 };
            double checksum = 0;

            for (int i = 0; i < size; ++i)
            {
                int x = points[i].Point.X;
                int y = points[i].Point.Y;
                differences[i] = image.Data[y, x, 0] - points[i].Intensity;
                checksum += Math.Abs(differences[i]);
                gradients[i] = new double[] { points[i].X, points[i].Y };
            }

            if (checksum > 0)
            {
                try
                {
                    shift = MultipleRegression.QR(gradients, differences, intercept: false);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in global tracking: {e}");
                }
            }
            else
            {
                Console.WriteLine($"Pattern not detected: checksum / size = {checksum / size}");
            }

            return (shift[0], shift[1]);
        }

        public static (double x, double y) CalculateShift(Image<Rgb, float> image, List<GradientPoint> points)
        {
            var size = points.Count;
            double[] differences = new double[size * 3];
            double[][] gradients = new double[size * 3][];
            double[] shift = new double[] { 0, 0 };
            double checksum = 0;

            for (int i = 0; i < size; ++i)
            {
                int x = points[i].Point.X;
                int y = points[i].Point.Y;
                differences[3 * i] = image.Data[y, x, 0] - points[i].Intensities.Red;
                differences[3 * i + 1] = image.Data[y, x, 1] - points[i].Intensities.Green;
                differences[3 * i + 2] = image.Data[y, x, 2] - points[i].Intensities.Blue;

                checksum += Math.Abs(differences[i]) + Math.Abs(differences[i + 1]) + Math.Abs(differences[i + 2]);

                gradients[3 * i] = new double[] { points[i].GradientX.Red, points[i].GradientY.Red };
                gradients[3 * i + 1] = new double[] { points[i].GradientX.Green, points[i].GradientY.Green };
                gradients[3 * i + 2] = new double[] { points[i].GradientX.Blue, points[i].GradientY.Blue };
            }

            if (checksum > 0)
            {
                try
                {
                    shift = MultipleRegression.QR(gradients, differences, intercept: false);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in global tracking: {e}");
                }
            }
            else
            {
                Console.WriteLine($"Pattern not detected: checksum / size = {checksum / size}");
            }

            return (shift[0], shift[1]);
        }

        public static bool FindGradientPoints<T>(Target target, Func<int, Image<T, byte>> getFrameImage) where T : struct, IColor
        {
            return FindGradientPoints<T>(target, (i) => getFrameImage(i).Convert<T, float>());
        }

        public static bool FindGradientPoints<T>(Target target, Func<int, Image<T, float>> getFrameImage) where T : struct, IColor
        {
            if (typeof(T) != typeof(Gray) && typeof(T) != typeof(Rgb)) return false;
            if (target.Reference == null || getFrameImage == null) return false;
            if ((typeof(T) == typeof(Rgb) && target.RgbReference != null) ||
                (typeof(T) == typeof(Gray) && target.GrayReference != null)) return true;

            var reference = getFrameImage(target.Reference.FrameNumber);
            if (reference == null) return false;

            if (target.ReferenceRange > 1)
            {
                var count = 1;
                for (int i = 1; i < target.ReferenceRange; ++i)
                {
                    var frame = getFrameImage(target.Reference.FrameNumber + i);
                    if (frame != null)
                    {
                        reference.Accumulate(frame);
                    }
                    reference = reference.ConvertScale<float>(1.0 / count, 0);
                }
            }
            var referenceRegion = System.Drawing.Rectangle.Round(target.RoundReference.Rectangle);
            TargetSearch search = null;
            if (typeof(T) == typeof(Rgb))
            {
                target.RgbReference = ImageAnalysis.PreparePattern(referenceRegion, reference) as Image<Rgb, float>;
                target.GrayReference = target.RgbReference?.Convert<Gray, float>();
                target.GradientPoints = ImageAnalysis.PrepareGradients(target.RgbReference);
                search = TargetSearch.Make(reference as Image<Rgb, float>, target.RgbReference, target.RoundReference);
            }
            else if (typeof(T) == typeof(Gray))
            {
                target.GrayReference = ImageAnalysis.PreparePattern(referenceRegion, reference) as Image<Gray, float>;
                target.GradientPoints = ImageAnalysis.PrepareGradients(target.GrayReference);
                search = TargetSearch.Make(reference as Image<Gray, float>, target.GrayReference, target.RoundReference);
            }

            if (search?.IsValid == true)
            {
                search.Find();
                if (target.ReferenceError == 0)
                {
                    target.ReferenceError = search.ErrorThreshold;
                }
            }
            return target.GradientPoints?.Count > 2;
        }

        public static bool FindGradientPoints<T>(Target target, Image<T, float> image) where T : struct, IColor
        {
            if (typeof(T) != typeof(Gray) && typeof(T) != typeof(Rgb)) return false;
            if (target.Reference == null || image == null) return false;
            if ((typeof(T) == typeof(Rgb) && target.RgbReference != null) ||
                (typeof(T) == typeof(Gray) && target.GrayReference != null)) return true;

            var referenceRegion = System.Drawing.Rectangle.Round(target.RoundReference.Rectangle);
            TargetSearch search = null;
            if (typeof(T) == typeof(Rgb))
            {
                target.RgbReference = ImageAnalysis.PreparePattern(referenceRegion, image) as Image<Rgb, float>;
                target.GrayReference = target.RgbReference?.Convert<Gray, float>();
                target.GradientPoints = ImageAnalysis.PrepareGradients(target.RgbReference);
                search = TargetSearch.Make(image as Image<Rgb, float>, target.RgbReference, target.RoundReference);
            }
            else if (typeof(T) == typeof(Gray))
            {
                target.GrayReference = ImageAnalysis.PreparePattern(referenceRegion, image) as Image<Gray, float>;
                target.GradientPoints = ImageAnalysis.PrepareGradients(target.GrayReference);
                search = TargetSearch.Make(image as Image<Gray, float>, target.GrayReference, target.RoundReference);
            }

            if (search?.IsValid == true)
            {
                search.Find();
                if (target.ReferenceError == 0)
                {
                    target.ReferenceError = search.ErrorThreshold;
                }
            }
            return target.HasGoodGradientPoints;
        }
    }
}
