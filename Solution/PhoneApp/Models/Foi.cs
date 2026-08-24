using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV;
using Emgu.CV.Structure;
using MicroVue.ViewModels;
using Newtonsoft.Json;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Models
{
    public partial class Foi : ObservableObject
    {
        #region Fields and Properties

        [ObservableProperty]
        int id;

        [ObservableProperty]
        string name = "FOI";

        [ObservableProperty]
        double frequency;

        [ObservableProperty]
        double binSize;

        [ObservableProperty]
        bool isNotProcessed = true;

        [ObservableProperty]
        bool isProcessing = false;

        [ObservableProperty]
        bool isReady = false;

        bool isSaving = false;
        [JsonIgnore]
        public bool IsSaving { get => isSaving; set {  SetProperty(ref isSaving, value); } }

        double progress;
        [JsonIgnore]
        public double Progress { get => progress; set { SetProperty(ref progress, value); } }

        [JsonIgnore]
        public CancellationTokenSource Cts { get; set; }

        public string SceneName { get; set; } = "Scene";

        public string RealImageFile { get; set; }

        public string ImagImageFile { get; set; }

        public string AverageImageFile { get; set; }

        Image<Bgr, float> realImage;
        [JsonIgnore]
        public Image<Bgr, float> RealImage
        {
            get
            {
                if (realImage == null && !string.IsNullOrEmpty(RealImageFile))
                {
                    realImage = new Image<Bgr, float>(GetFullPath(RealImageFile));
                }
                return realImage;
            }
            set
            {
                realImage = value;
                if (realImage != null)
                {
                    RealImageFile = GetFileName($"real_{Name}");
                    realImage.Save(GetFullPath(RealImageFile));
                }
                else
                {
                    if (!string.IsNullOrEmpty(RealImageFile))
                    {
                        File.Delete(GetFullPath(RealImageFile));
                        RealImageFile = null;
                    }
                }
            }
        }

        Image<Bgr, float> imagImage;
        [JsonIgnore]
        public Image<Bgr, float> ImagImage
        {
            get
            {
                if (imagImage == null && !string.IsNullOrEmpty(ImagImageFile))
                {
                    imagImage = new Image<Bgr, float>(GetFullPath(ImagImageFile));
                }
                return imagImage;
            }
            set
            {
                imagImage = value;
                if (imagImage != null)
                {
                    ImagImageFile = GetFileName($"imag_{Name}");
                    imagImage.Save(GetFullPath(ImagImageFile));
                }
                else
                {
                    if (!string.IsNullOrEmpty(ImagImageFile))
                    {
                        File.Delete(GetFullPath(ImagImageFile));
                        ImagImageFile = null;
                    }
                }
            }
        }

        Image<Bgr, float> averageImage;
        [JsonIgnore]
        public Image<Bgr, float> AverageImage
        {
            get
            {
                if (averageImage == null && !string.IsNullOrEmpty(AverageImageFile))
                {
                    averageImage = new Image<Bgr, float>(GetFullPath(AverageImageFile));
                }
                return averageImage;
            }
            set
            {
                averageImage = value;
                if (averageImage != null)
                {
                    AverageImageFile = GetFileName("average");
                    averageImage.Save(GetFullPath(AverageImageFile));
                }
                else
                {
                    if (!string.IsNullOrEmpty(AverageImageFile))
                    {
                        File.Delete(GetFullPath(AverageImageFile));
                        AverageImageFile = null;
                    }
                }
            }
        }

        Image<Bgr, float> minImage, maxImage;

        [ObservableProperty]
        double magnification = 100;

        public int NumberOfSamples { get; set; } = 20;

        public string VideoFile { get; set; }

        #endregion

        public Image<Bgr, byte> GetFrame(double phase) 
        {
            if (RealImage == null || ImagImage == null || AverageImage == null) return null;
            if (minImage == null || maxImage == null) MakeMinMaxImages();

            var magnifiedFrame = AverageImage + RealImage * (float)(Magnification * Math.Cos(phase)) - ImagImage * (float)(Magnification * Math.Sin(phase));
            if (minImage != null) CvInvoke.Max(minImage, magnifiedFrame, magnifiedFrame);
            if (maxImage != null) CvInvoke.Min(maxImage, magnifiedFrame, magnifiedFrame);
            var frame = magnifiedFrame?.Convert(f => (byte)Math.Max(0, Math.Min(f, 255)));
            magnifiedFrame?.Dispose();
            return frame;
        }

        public void MakeVideo(Action<double> progress = null)
        {
            try
            {
                var tempname = GetFullPath(GetFileName($"video_temp_{Name}", "mp4"));
                int fourcc = VideoWriter.Fourcc('H', '2', '6', '4');
                var width = AverageImage.Width;
                var height = AverageImage.Height;
                height = 16 * (height / 16);

                // beware - only few widths are acceptable at the moment: 640, 960, 1280, 1920.
                using (var writer = new VideoWriter(tempname, 0, fourcc, 20, new System.Drawing.Size(width, height), true))
                {
                    for (int i = 0; i < NumberOfSamples; ++i)
                    {
                        var phase = 2 * Math.PI * i / NumberOfSamples;
                        var frame = GetFrame(phase);

                        if (frame != null)
                        {
                            frame.ROI = new System.Drawing.Rectangle(0, 0, width, height);
                            writer.Write(frame);
                        }
                        else break;

                        progress?.Invoke((double)(i + 1) / NumberOfSamples); 
                        //Debug.WriteLine($"[Debug]: Write frame {i+1} to {tempname}, mag={Magnification}");
                        frame?.Dispose();
                        if (Cts?.Token.IsCancellationRequested == true) break;
                    }
                }

                if (Cts?.Token.IsCancellationRequested == true)
                {
                    File.Delete(tempname);
                }
                else
                {
                    var name = GetFileName($"video_{Name}", "mp4");
                    var path = GetFullPath(name);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tempname, path);
                    VideoFile = name;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in making foi video: {e}");
            }
        }

        public void Reset()
        {
            Remove();
            RealImage = null;
            ImagImage = null;
            AverageImage = null;
            VideoFile = null;
            IsNotProcessed = true;
            IsProcessing = false;
            IsReady = false;
            IsSaving = false;
        }

        public void Remove(bool deleteAverage = false)
        {
            Dispose();
            if (!string.IsNullOrWhiteSpace(RealImageFile)) File.Delete(GetFullPath(RealImageFile));
            if (!string.IsNullOrWhiteSpace(ImagImageFile)) File.Delete(GetFullPath(ImagImageFile));
            if (!string.IsNullOrWhiteSpace(AverageImageFile) && deleteAverage) File.Delete(GetFullPath(AverageImageFile));
            if (!string.IsNullOrWhiteSpace(VideoFile)) File.Delete(GetFullPath(VideoFile));
        }

        public void Dispose()
        {
            RealImage?.Dispose();
            ImagImage?.Dispose();
            AverageImage?.Dispose();
            minImage?.Dispose();
            maxImage?.Dispose();
        }

        void MakeMinMaxImages()
        {
            if (AverageImage == null) return;
            minImage = AverageImage.Erode(2);
            maxImage = AverageImage.Dilate(2);
        }

        public string GetFileName(string name, string ext = "tiff") => $"{name}.{ext}";
        public string GetFullPath(string name) => Scene.CurrentFolder + $"/{name}";
    }
}
