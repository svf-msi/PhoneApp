using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardLib
{
    public class ImageAnalysis
    {
        public static bool NormalizeDefault = true;

        public static int BlurSize = 5;

        public static Rgb EdgeColor = new Rgb(255, 0, 0);

        #region Image filtering

        public static Image<TColor, TDepth> Filter<TColor, TDepth>(Image<TColor, TDepth> image, ImageFilter imageFilter, Image<TColor, TDepth> prior = null)
            where TColor : struct, IColor
            where TDepth : new()
        {
            switch (imageFilter)
            {
                case ImageFilter.Smooth:
                    return Smooth(image);

                case ImageFilter.Sharpen:
                    return Sharpen(image);

                case ImageFilter.HiPass:
                    return HiPassSobel(image);

                case ImageFilter.Edges:
                    return Edges(image);

                case ImageFilter.Horizontal:
                    return H_Sobel(image, true);

                case ImageFilter.Vertical:
                    return V_Sobel(image, true);

                case ImageFilter.Median:
                    return Median(image);

                case ImageFilter.Velocity:
                    return Velocity(image, prior);

                default:
                    return image;
            }
        }

        /// <summary>
        /// Apply gaussian smoothing
        /// </summary>
        public static Image<TColor, TDepth> Smooth<TColor, TDepth>(Image<TColor, TDepth> image, int range = 5)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            if (range % 2 == 0) range += 1;
            return image.SmoothGaussian(range);
        }

        /// <summary>
        /// Apply unsharp masking
        /// </summary>
        public static Image<TColor, TDepth> Sharpen<TColor, TDepth>(Image<TColor, TDepth> image, double alpha = 1.5, double beta = -0.5, int range = 5)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            if (range % 2 == 0) range += 1;
            var original = image.Convert<TColor, float>();
            var mask = original - Smooth(original, range);
            var sharpen = original.AddWeighted(mask, alpha, beta, 0);
            return sharpen.Convert<TColor, TDepth>();
        }

        /// <summary>
        /// Apply gauss high pass filter
        /// </summary>
        public static Image<TColor, TDepth> HiPassSobel<TColor, TDepth>(Image<TColor, TDepth> image, int range = 5)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            if (range % 2 == 0) range += 1;
            var hs = image.Sobel(1, 0, range).AbsDiff(new TColor());
            var vs = image.Sobel(0, 1, range).AbsDiff(new TColor());
            var sum = hs.AddWeighted(vs, 0.5, 0.5, 0);
            return sum.Convert<TColor, TDepth>();
        }

        /// <summary>
        /// Apply gauss high pass filter
        /// </summary>
        public static Image<TColor, TDepth> HighPassGauss<TColor, TDepth>(Image<TColor, TDepth> image, int range = 5)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            if (range % 2 == 0) range += 1;
            var original = image.Convert<TColor, float>();
            var smooth = Smooth(original, range);
            var filtered = original - smooth;
            return filtered.Convert<TColor, TDepth>();
        }

        /// <summary>
        /// Apply 5x5 high pass filter
        /// </summary>
        public static Image<TColor, TDepth> HiPass5x5<TColor, TDepth>(Image<TColor, TDepth> image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            float[,] kernel = new float[,]
            {
                { -1, -1, -1, -1, -1 },
                { -1,  1,  2,  1, -1 },
                { -1,  2,  4,  2, -1 },
                { -1,  1,  2,  1, -1 },
                { -1, -1, -1, -1, -1 }
            };
            ConvolutionKernelF matrixKernel = new ConvolutionKernelF(kernel);
            var filtered = image.Convolution(matrixKernel);
            return filtered.Convert<TColor, TDepth>();
            //var abs = filtered.AbsDiff(new TColor());
            //CvInvoke.Normalize(abs, abs, 0, 255, NormType.MinMax, DepthType.Cv32F);
            //return abs.Convert<TColor, TDepth>();
        }

        /// <summary>
        /// Apply median filter
        /// </summary>
        public static Image<TColor, TDepth> Median<TColor, TDepth>(Image<TColor, TDepth> image, int range = 5)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            if (range % 2 == 0) range += 1;
            return image.SmoothMedian(range);
        }

        /// <summary>
        /// Find edges using custom thresholds
        /// </summary>
        public static Image<Gray, byte> GrayEdges<TColor, TDepth>(Image<TColor, TDepth> image, double lowThreshold, double highThreshold)
            where TColor : struct, IColor
            where TDepth : new()
        {
            return image?.Convert<Gray, byte>()?.Canny(lowThreshold, highThreshold);
        }

        /// <summary>
        /// Find edges automatically
        /// </summary>
        public static Image<Gray, byte> GrayEdges<TColor, TDepth>(Image<TColor, TDepth> image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            var gray = image.Convert<Gray, byte>();
            var mean = gray.GetAverage().Intensity;
            var lowThreshold = 1.0;
            var highThreshold = 3.0;
            return gray.Canny(mean * lowThreshold, mean * highThreshold);
        }

        /// <summary>
        /// Find edges automatically and color
        /// </summary>
        public static Image<TColor, TDepth> Edges<TColor, TDepth>(Image<TColor, TDepth> image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            var edges = GrayEdges(image);
            return image.Copy(edges);
        }

        /// <summary>
        /// Apply horizontal Sobel filter
        /// </summary>
        public static Image<TColor, TDepth> H_Sobel<TColor, TDepth>(Image<TColor, TDepth> image, bool abs = false, int range = 3)
            where TColor : struct, IColor
            where TDepth : new()
        {
            return Sobel(image, 1, 0, abs, range);
        }

        /// <summary>
        /// Apply vertical Sobel filter
        /// </summary>
        public static Image<TColor, TDepth> V_Sobel<TColor, TDepth>(Image<TColor, TDepth> image, bool abs = false, int range = 3)
            where TColor : struct, IColor
            where TDepth : new()
        {
            return Sobel(image, 0, 1, abs, range);
        }

        public static Image<TColor, TDepth> Sobel<TColor, TDepth>(Image<TColor, TDepth> image, int xorder = 1, int yorder = 0, bool abs = false, int range = 3)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image)) return null;
            if (range % 2 == 0) range += 1;
            var filtered = image.Sobel(xorder, yorder, range);
            if (abs) filtered = filtered.AbsDiff(new TColor());
            return filtered.Convert<TColor, TDepth>();
        }

        /// <summary>
        /// Calculate pixel velocity (difference) between images
        /// </summary>
        public static Image<TColor, TDepth> Velocity<TColor, TDepth>(Image<TColor, TDepth> image1, Image<TColor, TDepth> image2, bool normalize = false)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (!IsValid(image1) || !IsValid(image2)) return null;
            
            if (normalize)
            {
                var difference = image1.Convert<TColor, float>().AddWeighted(image2.Convert<TColor, float>(), 1, -1, 128);
                CvInvoke.Normalize(difference, difference, 0, 255, NormType.MinMax, DepthType.Cv32F);
                return difference.Convert<TColor, TDepth>();
            }
            else
            {
                var difference = image1.AddWeighted(image2, 1, -1, 128);
                return difference;
            }
        }

        #endregion

        #region Image mapping

        /// <summary>
        /// Normalize an image for mapping
        /// </summary>
        public static Image<Gray, byte> CreateIntensityMap(Image<Gray, float> image)
        {
            if (image == null) return null;
            var norm = new Image<Gray, float>(image.Size);
            image.MinMax(out var min, out var max, out var _, out var _);
            if (max[0] > 0)
                image = image * (255 / max[0]);
            return image.Convert<Gray, byte>();
        }

        /// <summary>
        /// Create logarithmic itensity map for image data
        /// </summary>
        public static Image<Gray, byte> CreateLogIntensityMap(float[,,] data)
        {
            if (data == null) return null;
            var image = new Image<Gray, float>(data);
            return CreateLogIntensityMap(image);
        }

        /// <summary>
        /// Create scaled logarithmic intesity map
        /// </summary>
        public static Image<Gray, byte> CreateLogIntensityMap(Image<Gray, float> image)
        {
            if (image == null) return null;
            var copy = image.SmoothGaussian(5);
            var noiseFloor = 0.05;
            copy = copy.Max(noiseFloor);
            copy = copy / noiseFloor;
            CvInvoke.Log(copy, copy);
            CvInvoke.Normalize(copy, copy, 0, 255, NormType.MinMax);
            return copy.Convert<Gray, byte>();
        }

        /// <summary>
        /// Create scaled square root intesity map
        /// </summary>
        public static Image<Gray, byte> CreateSqrtIntensityMap(Image<Gray, float> image)
        {
            if (image == null) return null;
            var copy = image.SmoothGaussian(5);
            CvInvoke.Sqrt(copy, copy);
            copy.MinMax(out var min, out var max, out var _, out var _);
            if (max[0] > 0)
                copy = copy * (255 / max[0]);
            return copy.Convert<Gray, byte>();
        }

        /// <summary>
        /// Create scaled square root intesity map
        /// </summary>
        public static Bitmap CreateSqrtColorMap(Image<Gray, float> image)
        {
            if (image == null) return new Bitmap(1, 1);
            var copy = CreateSqrtIntensityMap(image);
            return CreateColorMap(copy);
        }

        /// <summary>
        /// Create color intesity map for a grayscale image
        /// </summary>
        public static Bitmap CreateColorMap(Image<Gray, byte> image, ColorMapType mapType = ColorMapType.Jet)
        {
            if (image == null) return new Bitmap(1, 1);
            var colorMap = new Image<Bgr, byte>(image.Size);
            CvInvoke.ApplyColorMap(image, colorMap, mapType);
            return colorMap?.ToBitmap();
        }

        #endregion

        #region Feature detection

        /// <summary>
        /// Find potential corners in an image
        /// </summary>
        public static Image<Gray, byte> FindCorners(Image<Gray, float> image)
        {
            if (image == null) return null;
            var harrisCorners = new Image<Gray, float>(image.Size);
            CvInvoke.CornerHarris(image, harrisCorners, 4, 3, 0.04);
            harrisCorners.MinMax(out var min, out var max, out _, out _);
            var thresh = max[0] / 500;
            var inner = harrisCorners.ThresholdBinary(new Gray(thresh), new Gray(255)).Convert<Gray, byte>();
            return inner.Erode(1).Dilate(2);
        }

        /// <summary>
        /// Find edges in a grayscale image
        /// </summary>
        public static Image<Gray, byte> CreateEdgeMask(Image<Gray, float> image)
        {
            if (image == null) return null;
            var reference = image.Convert<Gray, byte>();
            return CreateEdgeMask(reference);
        }

        /// <summary>
        /// Find edges in a grayscale image
        /// </summary>
        public static Image<Gray, byte> CreateEdgeMask(Image<Gray, byte> image)
        {
            if (image == null) return null;
            var mean = image.GetAverage().Intensity;
            var lowThreshold = 1.0 * mean;
            var highThreshold = 3.0 * mean;
            return CreateEdgeMask(image, lowThreshold, highThreshold);
        }

        /// <summary>
        /// Find edges in a grayscale image
        /// </summary>
        public static Image<Gray, byte> CreateEdgeMask(Image<Gray, byte> image, double lowThreshold, double highThreshold)
        {
            if (image == null) return null;
            var reference = image.Convert<Gray, byte>();
            return reference.Canny(lowThreshold, highThreshold);
        }

        /// <summary>
        /// Create color image bitmap for an edge image
        /// </summary>
        public static Bitmap CreateEdgeMap(Image<Gray, byte> image, Bgr edgeColor = default)
        {
            if (edgeColor.Equals(default)) edgeColor = new Bgr(0, 255, 0);
            var mask = image.Convert<Bgr, byte>();
            var edges = new Image<Bgr, byte>(mask.Width, mask.Height, edgeColor);
            CvInvoke.BitwiseAnd(edges, mask, edges);
            var edgeMap = edges.ToBitmap();
            edgeMap.MakeTransparent(Color.FromArgb(0, 0, 0));
            return edgeMap;
        }

        /// <summary>
        /// Find edges and create color image bitmap for edges
        /// </summary>
        public static Bitmap CreateEdgeMap<TColor, TDepth>(Image<TColor, TDepth> image, Rgb edgeColor = default, int dilation = 0)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (image == null) return null;
            if (edgeColor.Equals(default)) edgeColor = EdgeColor;
            var edges = GrayEdges(image);
            if (dilation > 0)
            {
                edges = edges.Dilate(dilation);
            }
            var mask = edges.Convert<Rgb, byte>();
            var colorEdges = new Image<Rgb, byte>(mask.Width, mask.Height, edgeColor);
            CvInvoke.BitwiseAnd(colorEdges, mask, colorEdges);
            var edgeMap = colorEdges.ToBitmap();
            edgeMap.MakeTransparent(Color.FromArgb(0, 0, 0));
            return edgeMap;
        }

        /// <summary>
        /// Locate all found edge points
        /// </summary>
        public static HashSet<int> FindEdges(Image<Gray, byte> edgeMask)
        {
            var edges = new HashSet<int>();

            if (edgeMask != null)
            {

                VectorOfPoint locations = new VectorOfPoint();
                CvInvoke.FindNonZero(edgeMask, locations);

                for (int i = 0; i < locations.Size; ++i)
                {
                    var point = locations[i];
                    if (point != null && edgeMask != null)
                    {
                        var index = point.X + point.Y * edgeMask.Width;
                        edges.Add(index);
                    }
                }
            }
            //Console.WriteLine($"Found {edges.Count} edge points");
            return edges;
        }

        /// <summary>
        /// Locate all found edge points
        /// </summary>
        public static List<List<Point>> GroupEdges(Image<Gray, byte> edgeMask, Image<Gray, float> gradientDirection, int edgeSize = 10)
        {
            var edges = new List<List<Point>>();

            if (edgeMask != null)
            {
                try
                {
                    VectorOfPoint points = new VectorOfPoint();
                    CvInvoke.FindNonZero(edgeMask, points);
                    var removed = new bool[edgeMask.Width, edgeMask.Height];

                    for (int i = 0; i < points.Size; ++i)
                    {
                        var point = points[i];
                        if (!removed[point.X, point.Y])
                        {
                            var edge = new List<Point>();
                            var gradient = gradientDirection.Data[point.Y, point.X, 0];
                            AddPoint(point, edge, edgeMask, gradient, gradientDirection, removed, edgeSize);
                            if (edge.Count >= edgeSize)
                            {
                                edges.Add(edge);
                            }
                        }
                    }

                    //Console.WriteLine($"Points:");
                    //Console.WriteLine(Utils.ToString(points));
                    //Console.WriteLine($"Edge groups:");
                    //Console.WriteLine(Utils.ToString(edges));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in group edges: {e}");
                }
            }
            return edges;
        }

        static double tolerance = Math.PI / 4;
        static List<(int x, int y)> moves = new List<(int x, int y)> { (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1) };

        static void AddPoint(Point point, List<Point> edge, Image<Gray, byte> edgeMask, float gradient, Image<Gray, float> gradients, bool[,] removed, int size = 10)
        {
            edge.Add(point);
            removed[point.X, point.Y] = true;
            if (edge.Count >= size) return;

            foreach (var move in moves)
            {
                var x = point.X + move.x;
                var y = point.Y + move.y;
                if (x >= 0 && x < edgeMask.Width && y >= 0 && y < edgeMask.Height)
                {
                    var gradientDiff = gradients.Data[y, x, 0] - gradient;
                    var gradientIsGood = Math.Abs(gradientDiff) < tolerance || Math.Abs(Math.Abs(gradientDiff) - 2 * Math.PI) < tolerance;
                    if (edgeMask.Data[y, x, 0] > 0 && !removed[x, y] && gradientIsGood)
                    {
                        AddPoint(new Point(x, y), edge, edgeMask, gradient, gradients, removed, size);
                        if (edge.Count >= size) return;
                    }
                }
            }
        }

        #endregion

        #region Processing

        public static Image<TColor, float> BlurImage<TColor, TDepth>(Image<TColor, TDepth> image, int range)
            where TColor : struct, IColor
            where TDepth : new()
        {
            using (var grayImage = image.Convert<TColor, float>())
            {
                if (range % 2 == 0) range += 1;
                if (grayImage != null)
                {
                    return grayImage.SmoothGaussian(range);
                }
                else
                    return null;
            }
        }

        public static Image<TColor, float> BlurCroppedImage<TColor>(Image<TColor, float> image, int range, Rectangle region, int inflation = 10)
            where TColor : struct, IColor
        {
            if (image == null) return null;

            var x = Math.Min(inflation, region.X);
            var y = Math.Min(inflation, region.Y);
            var width = region.Width;
            var height = region.Height;
            region.Inflate(inflation, inflation);
            region.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            image.ROI = region;
            if (range % 2 == 0) range += 1;
            using (var smooth = image.SmoothGaussian(range))
            {
                image.ROI = Rectangle.Empty;
                smooth.ROI = new Rectangle(x, y, width, height);
                return smooth.Copy();
            }
        }

        public static Image<TColor, float> PreparePattern<TColor>(Rectangle region, Image<TColor, float> image)
            where TColor : struct, IColor
        {
            return PreparePattern(region, image, NormalizeDefault);
        }

        public static Image<TColor, float> PreparePattern<TColor>(Rectangle region, Image<TColor, float> image, bool normalize)
            where TColor : struct, IColor
        {
            try
            {
                var pattern = BlurCroppedImage(image, BlurSize, region);
                if (normalize && pattern != null)
                {
                    pattern.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
                    var maximum = maxValues[0];
                    if (maxValues.Length > 2)
                    {
                        maximum = (maxValues[0] + maxValues[1] + maxValues[2]) / 3;
                    }
                    if (maximum > 0)
                        pattern = pattern.Convert((float f) => (float)(f / maximum));
                }
                return pattern;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in making pattern: {e}");
                return null;
            }
        }

        public static List<GradientPoint> PrepareGradients(Image<Gray, float> image)
        {
            float maxGradient = 0.0f;
            var referenceGradientX = new Image<Gray, float>(image.Size);
            var referenceGradientY = new Image<Gray, float>(image.Size);
            CvInvoke.Sobel(image, referenceGradientX, DepthType.Cv32F, 1, 0, 1);
            CvInvoke.Sobel(image, referenceGradientY, DepthType.Cv32F, 0, 1, 1);
            var gradientPoints = new List<GradientPoint>();
            for (int r = 0; r < image.Height; ++r)
            {
                for (int c = 0; c < image.Width; ++c)
                {
                    var gradientX = referenceGradientX.Data[r, c, 0] / 2; // Factor of 2 - correction to Sobel
                    var gradientY = referenceGradientY.Data[r, c, 0] / 2;
                    float gradient = (float)Math.Sqrt(gradientX * gradientX + gradientY * gradientY);
                    float intensity = image.Data[r, c, 0];
                    maxGradient = Math.Max(maxGradient, gradient);

                    var gradientPoint = new GradientPoint()
                    {
                        Point = new System.Drawing.Point(c, r),
                        X = -gradientX,
                        Y = -gradientY,
                        Gradient = gradient,
                        Intensity = intensity
                    };
                    gradientPoints.Add(gradientPoint);
                }
            }

            return gradientPoints?.Where(p => p.Gradient > maxGradient / 2)?.ToList();
        }

        public static List<GradientPoint> PrepareGradients(Image<Rgb, float> image)
        {
            float maxGradient = 0.0f;
            var referenceGradientX = new Image<Rgb, float>(image.Size);
            var referenceGradientY = new Image<Rgb, float>(image.Size);
            CvInvoke.Sobel(image, referenceGradientX, DepthType.Cv32F, 1, 0, 1);
            CvInvoke.Sobel(image, referenceGradientY, DepthType.Cv32F, 0, 1, 1);
            var gray = image.Convert<Gray, float>();
            var grayGradientX = new Image<Gray, float>(image.Size);
            var grayGradientY = new Image<Gray, float>(image.Size);
            CvInvoke.Sobel(gray, grayGradientX, DepthType.Cv32F, 1, 0, 1);
            CvInvoke.Sobel(gray, grayGradientY, DepthType.Cv32F, 0, 1, 1);

            var gradientPoints = new List<GradientPoint>();
            for (int r = 0; r < image.Height; ++r)
            {
                for (int c = 0; c < image.Width; ++c)
                {
                    var grayX = -grayGradientX.Data[r, c, 0] / 2; // Factor of 2 - correction to Sobel
                    var grayY = -grayGradientY.Data[r, c, 0] / 2;
                    float gradient = (float)Math.Sqrt(grayX * grayX + grayY * grayY);
                    float intensity = gray.Data[r, c, 0];
                    maxGradient = Math.Max(maxGradient, gradient);

                    var gradientX = (-referenceGradientX.Data[r, c, 0] / 2, -referenceGradientX.Data[r, c, 1] / 2, -referenceGradientX.Data[r, c, 2] / 2);
                    var gradientY = (-referenceGradientY.Data[r, c, 0] / 2, -referenceGradientY.Data[r, c, 1] / 2, -referenceGradientY.Data[r, c, 2] / 2);
                    var intensities = (image.Data[r, c, 0], image.Data[r, c, 1], image.Data[r, c, 2]);

                    var gradientPoint = new GradientPoint()
                    {
                        Point = new Point(c, r),
                        X = grayX,
                        Y = grayY,
                        Gradient = gradient,
                        Intensity = intensity,
                        GradientX = gradientX,
                        GradientY = gradientY,
                        Intensities = intensities
                    };
                    gradientPoints.Add(gradientPoint);
                }
            }

            return gradientPoints?.Where(p => p.Gradient > maxGradient / 2)?.ToList();
        }

        #endregion

        #region Miscellaneous

         public static Image<TColor, TDepth> Transform<TColor, TDepth>(Image<TColor, TDepth> image, TransformPoint transform)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (transform == null || image == null) return image;

            var cos = (float)Math.Cos(transform.Angle);
            var sin = (float)Math.Sin(transform.Angle);
            var matrix = new Matrix<float>(new float[,] { { cos, -sin, (float)transform.X }, { sin, cos, (float)transform.Y } });
            var transformedImage = new Image<TColor, TDepth>(image.Size);
            CvInvoke.WarpAffine(image, transformedImage, matrix, image.Size);
            return transformedImage;
        }

        public static Mat Transform(Mat image, TransformPoint transform)
        {
            if (transform == null || image == null) return image;

            var cos = (float)Math.Cos(transform.Angle);
            var sin = (float)Math.Sin(transform.Angle);
            var matrix = new Matrix<float>(new float[,] { { cos, -sin, (float)transform.X }, { sin, cos, (float)transform.Y } });
            var transformedImage = new Mat();
            CvInvoke.WarpAffine(image, transformedImage, matrix, image.Size);
            return transformedImage;
        }

        public static TransformPoint Transform(TransformPoint point, TransformPoint transform)
        {
            if (transform == null || point == null) return point;

            var cos = Math.Cos(transform.Angle);
            var sin = Math.Sin(transform.Angle);
            return new TransformPoint()
            {
                X = point.X * cos - point.Y * sin + transform.X,
                Y = point.Y * cos + point.X * sin + transform.Y,
            };
        }

        public static Bitmap Add(Bitmap bitmap1, Bitmap bitmap2)
        {
            if (bitmap1 == null || bitmap2 == null || bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height) return bitmap1;
            var graphics = Graphics.FromImage(bitmap1);
            graphics.DrawImage(bitmap2, new Point());
            return bitmap1;
        }

        public static bool FilterFrequency<T>(double frequency, double frameRate, int length, Func<int, Image<Gray, T>> grabFrame,
            out Image<Gray, float> real, out Image<Gray, float> imag) where T : new()
        {
            real = null;
            imag = null;
            if (grabFrame == null || length == 0) return false;

            var bin = Math.Round(frequency * length / frameRate);
            try
            {
                var sample = grabFrame(0);
                if (sample == null) return false;

                var width = sample.Width;
                var height = sample.Height;
                int sections = 4;

                if (sections > 1)
                {
                    int sectionLength = (int)Math.Ceiling((double)length / sections);
                    var tasks = new List<Task>();
                    var reImages = new List<Image<Gray, double>>();
                    var imImages = new List<Image<Gray, double>>();
                    for (int k = 0; k < sections; ++k)
                    {
                        var section = k;
                        reImages.Add(new Image<Gray, double>(width, height));
                        imImages.Add(new Image<Gray, double>(width, height));
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                int sectionStart = section * sectionLength;
                                int sectionEnd = Math.Min((section + 1) * sectionLength, length);
                                for (int i = sectionStart; i < sectionEnd; ++i)
                                {
                                    var frame = grabFrame(i).Convert<Gray, double>();
                                    var phase = 2 * Math.PI * i * bin / length;
                                    CvInvoke.Accumulate(frame * (Math.Cos(phase) * 2), reImages[section]);
                                    CvInvoke.Accumulate(frame * (Math.Sin(-phase) * 2), imImages[section]);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error in processing FOIs: {e}");
                            }
                        }));
                    }

                    Task.WhenAll(tasks).Wait();
                    for (int k = 1; k < sections; ++k)
                    {
                        reImages[0] += reImages[k];
                        imImages[0] += imImages[k];
                    }
                    real = reImages[0].ConvertScale<float>(1.0 / length, 0);
                    imag = imImages[0].ConvertScale<float>(1.0 / length, 0);
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        var frame = grabFrame(i).Convert<Gray, float>();
                        var phase = 2 * Math.PI * i * bin / length;
                        if (real == null || imag == null)
                        {
                            real = new Image<Gray, float>(width, height);
                            imag = new Image<Gray, float>(width, height);
                        }
                        CvInvoke.Accumulate(frame * Math.Cos(phase) * 2, real);
                        CvInvoke.Accumulate(frame * Math.Sin(-phase) * 2, imag);
                    }

                    real = real.ConvertScale<float>(1.0 / length, 0);
                    imag = imag.ConvertScale<float>(1.0 / length, 0);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in image filtering: {e}");
                return false;
            }
        }

        public static void FixRgbBorder<TDepth>(Image<Gray, TDepth> image)
            where TDepth : new()
        {
            if (image == null) return;
            var width = image.Width;
            var height = image.Height;
            if (width < 2 || height < 2) return;
            image.Data[0, width - 1, 0] = image.Data[1, width - 2, 0];
            for (int c = 0; c < width - 1; ++c)
            {
                image.Data[0, c, 0] = image.Data[1, c, 0];
                image.Data[height - 1, c, 0] = image.Data[height - 2, c, 0];
            }
            for (int r = 1; r < height; ++r)
            {
                image.Data[r, width - 1, 0] = image.Data[r, width - 2, 0];
            }
        }

        public static float FindMaximum(Image<Gray, float> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Gray, float>();
            double[] minValues, maxValues;
            Point[] minLocations, maxLocations;
            sample.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
            return (float)maxValues[0];
        }

        public static (float Red, float Green, float Blue) FindMaximum(Image<Rgb, float> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Rgb, float>();
            double[] minValues, maxValues;
            Point[] minLocations, maxLocations;
            sample.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
            return ((float)maxValues[0], (float)maxValues[1], (float)maxValues[2]);
        }

        public static int SaturationCount(Image<Gray, byte> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Gray, byte>();
            sample = sample.ThresholdBinary(new Gray(254), new Gray(1));
            return (int)sample.GetSum().MCvScalar.V0;
        }

        public static (int Red, int Green, int Blue) SaturationCount(Image<Rgb, byte> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Rgb, byte>();
            sample = sample.ThresholdBinary(new Rgb(254, 254, 254), new Rgb(1, 1, 1));
            var sum = sample.GetSum().MCvScalar;
            return ((int)sum.V0, (int)sum.V1, (int)sum.V2);
        }

        public static double Average(Image<Gray, byte> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Gray, byte>();
            return sample.GetAverage().MCvScalar.V0;
        }

        public static (double Red, double Green, double Blue) Average(Image<Rgb, byte> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Rgb, byte>();
            var average = sample.GetAverage().MCvScalar;
            return (average.V0, average.V1, average.V2);
        }

        public static (double Hue, double Saturation, double Value) AverageHSV(Image<Rgb, byte> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Hsv, byte>();
            var average = sample.GetAverage().MCvScalar;
            return (average.V0, average.V1, average.V2);
        }

        public static double FrameDifference(Image<Rgb, byte> image1, Rectangle rect1, Image<Rgb, byte> image2, Rectangle rect2)
        {
            if (rect1.Size != rect2.Size)
            {
                Console.WriteLine($"Frame difference requested with different sample sizes");
                return 0;
            }

            // Calculate mean squared error
            var sample1 = new Mat(image1.Mat, rect1).ToImage<Gray, byte>();
            var sample2 = new Mat(image2.Mat, rect2).ToImage<Gray, byte>();
            Mat diff = new Mat();
            CvInvoke.AbsDiff(sample1, sample2, diff);
            CvInvoke.Pow(diff, 2, diff);
            var mean = CvInvoke.Mean(diff);
            return Math.Sqrt(mean.V0);
        }

        public static double FrameBurn(Image<Rgb, byte> image1, Rectangle rect1, Image<Rgb, byte> image2, Rectangle rect2)
        {
            if (rect1.Size != rect2.Size)
            {
                Console.WriteLine($"Frame difference requested with different sample sizes");
                return 0;
            }

            //Intensity option:
            var sample1 = new Mat(image1.Mat, rect1).ToImage<Gray, byte>().ConvertScale<float>(1.0 / 255, 0);
            var sample2 = new Mat(image2.Mat, rect2).ToImage<Gray, byte>().ConvertScale<float>(1.0 / 255, 0);

            //Red option:
            //var sample1 = image1.Split()[0].ConvertScale<float>(1.0 / 255, 0);
            //var sample2 = image2.Split()[0].ConvertScale<float>(1.0 / 255, 0);
            CvInvoke.Pow(sample1, 2, sample1);
            CvInvoke.Pow(sample2, 2, sample2);
            var diff = new Image<Gray, float>(image1.Size);
            CvInvoke.Subtract(sample1, sample2, diff);
            var mean = CvInvoke.Mean(diff);
            return mean.V0;
        }

        public static double HistogramDifference(Image<Rgb, byte> image1, Rectangle rect1, Image<Rgb, byte> image2, Rectangle rect2)
        {
            if (rect1.Size != rect2.Size)
            {
                Console.WriteLine($"Histogram difference requested with different sample sizes");
                return 0;
            }

            var sample1 = new Mat(image1.Mat, rect1);
            var sample2 = new Mat(image2.Mat, rect2);
            Mat sample1HSV = new Mat(), sample2HSV = new Mat();
            CvInvoke.CvtColor(sample1, sample1HSV, ColorConversion.Rgb2Hsv);
            CvInvoke.CvtColor(sample2, sample2HSV, ColorConversion.Rgb2Hsv);
            int hBins = 50, sBins = 60;
            int[] histSize = { hBins, sBins };
            float[] ranges = { 0, 180, 0, 256 };
            int[] channels = { 0, 1 };

            VectorOfMat vou1 = new VectorOfMat(), vou2 = new VectorOfMat();
            vou1.Push(sample1);
            vou2.Push(sample2);

            Mat histTest1 = new Mat(), histTest2 = new Mat();
            CvInvoke.CalcHist(vou1, channels, new Mat(), histTest1, histSize, ranges, false);
            CvInvoke.Normalize(histTest1, histTest1, 0, 1, NormType.MinMax);
            CvInvoke.CalcHist(vou2, channels, new Mat(), histTest2, histSize, ranges, false);
            CvInvoke.Normalize(histTest2, histTest2, 0, 1, NormType.MinMax);

            double result = CvInvoke.CompareHist(histTest1, histTest2, HistogramCompMethod.Correl);
            return 1.0 - result; // no difference is 1.0
        }

        public static double Entropy(Image<Gray, byte> image, Rectangle rect)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Gray, byte>();

            DenseHistogram hist = new DenseHistogram(256, new RangeF(0, 256));
            hist.Calculate(new Image<Gray, byte>[] { sample }, false, null);
            double totalPixels = sample.Width * sample.Height;

            double entropy = 0.0;
            float[,] histData = (float[,])hist.GetData();

            for (int i = 0; i < 256; i++)
            {
                double count = histData[i, 0];

                if (count > 0)
                {
                    double probability = count / totalPixels;
                    entropy -= probability * Math.Log(probability, 2);
                }
            }

            return entropy;
        }

        public static double PixelsAboveLuminanceThreshold(Image<Gray, byte> image, Rectangle rect, byte threshold)
        {
            var sample = new Mat(image.Mat, rect).ToImage<Gray, byte>();

            double totalPixels = sample.Width * sample.Height;

            Mat thresholded = new Mat();
            CvInvoke.Threshold(sample, thresholded, threshold, 255, ThresholdType.Binary);
            double pixelCount = CvInvoke.CountNonZero(thresholded);

            return pixelCount / totalPixels;
        }

        public static bool IsValid<TColor, TDepth>(Image<TColor, TDepth> image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            try
            {
                return image != null && image.Width > 0 && image.Height > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }


    public class GradientPoint
    {
        public Point Point { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Gradient { get; set; }
        public float Intensity { get; set; }
        public (float Red, float Green, float Blue) GradientX { get; set; }
        public (float Red, float Green, float Blue) GradientY { get; set; }
        public (float Red, float Green, float Blue) Intensities { get; set; }

    }

    public class TransformPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }

        public override string ToString()
        {
            return $"X:{X}, Y:{Y}, Angle:{Angle}";
        }
    }

    public enum ImageFilter { None, Smooth, Sharpen, Edges, HiPass, Median, Horizontal, Vertical, Velocity }

    public enum BlurType
    {
        None,
        Average,
        Gaussian,
        Median,
        Bilateral
    }
}
