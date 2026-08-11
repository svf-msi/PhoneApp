using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV;
using Emgu.CV.Structure;
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
                    realImage = new Image<Bgr, float>(RealImageFile);
                }
                return realImage;
            }
            set
            {
                realImage = value;
                if (realImage != null)
                {
                    RealImageFile = GetImageFileName($"_{Name}_real");
                    realImage.Save(RealImageFile);
                }
                else
                {
                    if (!string.IsNullOrEmpty(RealImageFile))
                    {
                        File.Delete(RealImageFile);
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
                    imagImage = new Image<Bgr, float>(ImagImageFile);
                }
                return imagImage;
            }
            set
            {
                imagImage = value;
                if (imagImage != null)
                {
                    ImagImageFile = GetImageFileName($"_{Name}_imag");
                    imagImage.Save(ImagImageFile);
                }
                else
                {
                    if (!string.IsNullOrEmpty(ImagImageFile))
                    {
                        File.Delete(ImagImageFile);
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
                    averageImage = new Image<Bgr, float>(AverageImageFile);
                }
                return averageImage;
            }
            set
            {
                averageImage = value;
                if (averageImage != null)
                {
                    AverageImageFile = GetImageFileName("average");
                    averageImage.Save(AverageImageFile);
                }
                else
                {
                    if (!string.IsNullOrEmpty(AverageImageFile))
                    {
                        File.Delete(AverageImageFile);
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

        Video_MP4 video;
        [JsonIgnore]
        public Video_MP4 Video
        {
            get
            {
                if (video == null && !string.IsNullOrWhiteSpace(VideoFile))
                {
                    video = new Video_MP4(VideoFile);
                }
                return video;
            }
            protected set => video = value;
        }

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
                var tempname = GetVideoFileName($"_{Name}_video_temp");
                int fourcc = VideoWriter.Fourcc('H', '2', '6', '4');
                var width = AverageImage.Width;
                var height = AverageImage.Height;
                using (var writer = new VideoWriter(tempname, 0, fourcc, 20, new System.Drawing.Size(width, height), true))
                {
                    for (int i = 0; i < NumberOfSamples; ++i)
                    {
                        var phase = 2 * Math.PI * i / NumberOfSamples;
                        var frame = GetFrame(phase);
                        if (frame != null)
                        {
                            writer.Write(frame);
                        }
                        frame?.Dispose();
                        progress?.Invoke((double)(i + 1) / NumberOfSamples); 
                        Debug.WriteLine($"Write frame {i+1} to {tempname}, mag={Magnification}");
                        if (Cts?.Token.IsCancellationRequested == true) break;
                    }
                }

                if (Cts?.Token.IsCancellationRequested == true)
                {
                    File.Delete(tempname);
                }
                else
                {
                    var name = GetVideoFileName($"_{Name}_video");
                    if (File.Exists(name)) File.Delete(name);
                    File.Move(tempname, name);
                    VideoFile = name;
                    Video = new Video_MP4(name);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in making foi video: {e}");
            }
        }

        public void Remove()
        {
            Dispose();
            if (!string.IsNullOrWhiteSpace(RealImageFile)) File.Delete(RealImageFile);
            if (!string.IsNullOrWhiteSpace(ImagImageFile)) File.Delete(ImagImageFile);
            if (!string.IsNullOrWhiteSpace(AverageImageFile)) File.Delete(AverageImageFile);
            if (!string.IsNullOrWhiteSpace(VideoFile)) File.Delete(VideoFile);

            Debug.WriteLine(Utilities.ListFolderContents(App.FoiDataFolder));
        }

        public void Dispose()
        {
            RealImage?.Dispose();
            ImagImage?.Dispose();
            AverageImage?.Dispose();
            minImage?.Dispose();
            maxImage?.Dispose();
            Video?.Dispose();
        }

        void MakeMinMaxImages()
        {
            if (AverageImage == null) return;
            minImage = AverageImage.Erode(2);
            maxImage = AverageImage.Dilate(2);
        }

        string GetImageFileName(string suffix, string ext = "tiff") => App.FoiDataFolder + $"{SceneName}_{suffix}.{ext}";

        string GetVideoFileName(string suffix, string ext = "mp4") => App.FoiVideoFolder + $"{SceneName}_{suffix}.{ext}";
    }
}
