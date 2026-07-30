using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Reg;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StandardLib
{
    public class Video_MP4
    {
        VideoCapture? videoCapture;
        Mat? image;
        int currentFrame = -1;

        int rotation = 0;
        public string? Path { get; set; }
        public bool IsValid { get; set; } = false;

        private double timeInterval = 1;
        public double TimeInterval { get => timeInterval; set { timeInterval = value; } }

        public int Length { get; set; } = 0;
        //public int Rotation { get => rotation; set { rotation = value; SetSize(); } }
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public double FPS { get; protected set; }
        public bool UseTransform { get; set; } = false;
        public Dictionary<int, TransformPoint> Transforms { get; set; }

        public Video_MP4() { }

        public Video_MP4(string filename, double timeInterval = 1)
        {
            Path = filename;
            TimeInterval = timeInterval;
            Initialize();
        }

        public void Initialize()
        {
            if (!File.Exists(Path))
            {
                Debug.WriteLine($"[Debug]: File {Path} does not exists.");
                return;
            }

            Dispose();
            videoCapture = new VideoCapture(Path);
            Length = (int)videoCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
            SetSize();
            IsValid = true;
        }

        public void Count()
        {
            var count = 0;
            while (ReadFrameMat(out Mat image))
            {
                ++count;
                image.Dispose();
            }
            Length = count;
        }

        public void Reset()
        {
            videoCapture?.Dispose();
            videoCapture = new VideoCapture(Path);
        }

        public bool ReadFrameMat(out Mat image)
        {
            image = new Mat();
            try
            {
                return videoCapture?.Read(image) ?? false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in reading frame: {e}");
                return false;
            }
        }

        public bool ReadFrame(out Image<Gray, byte> image)
        {
            image = null;
            try
            {
                if (ReadFrameMat(out Mat mat))
                {
                    Mat grayMat = new Mat();
                    CvInvoke.CvtColor(mat, grayMat, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                    image = grayMat.ToImage<Gray, byte>();
                    return true;
                }
                else return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in reading frame: {e}");
                return false;
            }
        }

        void SetImage(int i)
        {
            if (currentFrame != i || image == null)
            {
                Try(() =>
                {
                    if (videoCapture != null)
                    {
                        if (i != videoCapture.Get(CapProp.PosFrames))
                            videoCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, i);
                        image = videoCapture.QueryFrame();

                        if (UseTransform && Transforms != null && Transforms.ContainsKey(i))
                        {
                            var oldImage = image;
                            image = ImageAnalysis.Transform(image, Transforms[i]);
                            oldImage?.Dispose();
                        }
                    }
                });

                currentFrame = i;
            }
        }

        public SKBitmap? GetFrame(int i)
        {
            SetImage(i);

            //if (Rotation != 0)
            //{
            //    var rotImage = image?.ToImage<Bgr, byte>();
            //    rotImage = rotImage?.Rotate(90 * Rotation, new Bgr(), false);
            //    return rotImage?.Bit;
            //}

            return ImageAnalysis.ToSKBitmap(image);
        }

        public void Dispose() 
        {
            try
            {
                image?.Dispose();
                videoCapture?.Dispose();
                currentFrame = -1;
                image = null;
                videoCapture = null;
            }
            catch (Exception err) { Console.WriteLine(err); }
        }

        public Mat? GetMat(int i)
        {
            if (currentFrame != i || image == null)
            {
                Try(() =>
                {
                    if (videoCapture != null)
                    {
                        if (i != videoCapture.Get(CapProp.PosFrames))
                            videoCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, i);
                        image = videoCapture.QueryFrame();

                        if (UseTransform && Transforms != null && Transforms.ContainsKey(i))
                        {
                            var oldImage = image;
                            image = ImageAnalysis.Transform(image, Transforms[i]);
                            oldImage?.Dispose();
                        }
                    }
                });

                currentFrame = i;
            }

            //if (image != null && Rotation != 0)
            //{
            //    Mat rotatedImage = new Mat();
            //    RotateFlags rotation = RotateFlags.Rotate180;
            //    if (Rotation % 2 != 0) rotation = Rotation < 0 ? RotateFlags.Rotate90CounterClockwise : RotateFlags.Rotate90Clockwise;
            //    CvInvoke.Rotate(image, rotatedImage, rotation);
            //    return rotatedImage;
            //}

            return image;
        }

        public Image<Rgb, byte>? GetRgbImage(int i)
        {
            if (Length == 0 || !IsInRange(i)) return null;
            return GetMat(i)?.ToImage<Rgb, byte>();
        }

        public Image<Gray, byte>? GetGrayImage(int i)
        {
            if (Length == 0 || !IsInRange(i)) return null;
            var image = GetMat(i);
            Mat grayImage = new Mat();
            CvInvoke.CvtColor(image, grayImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            return grayImage.ToImage<Gray, byte>();
        }

        bool IsInRange(int i)
        {
            return i >= 0 && i < Length;
        }

        void SetSize()
        {
            if (videoCapture == null) return;
            var width = (int)videoCapture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth);
            var height = (int)videoCapture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight);
            Width = width; //Rotation % 2 == 0 ? width : height;
            Height = height; //Rotation % 2 == 0 ? height : width;
        }

        void Try(Action action)
        {
            try { action(); }
            catch (Exception e) { Console.WriteLine($"Error in video: {e}"); }
        }

    }

}
