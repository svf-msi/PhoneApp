using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emgu.CV;
using Emgu.CV.Structure;
using MicroVue.Models;
using Newtonsoft.Json;
using SkiaSharp;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace MicroVue.ViewModels
{
    [QueryProperty(nameof(SceneItem), "SceneItem")]
    public partial class AnalysisViewModel : ObservableObject
    {
        [ObservableProperty]
        List<string> targetColors = new List<string> { "Red", "Green", "Blue", "Yellow", "Teal", "Purple" };

        [ObservableProperty]
        SceneItem sceneItem = new SceneItem();

        [ObservableProperty]
        Scene scene;

        [ObservableProperty]
        string sceneName;

        [ObservableProperty]
        string videoPath;

        [ObservableProperty]
        private ObservableCollection<string> speeds = new() { "x1", "x2", "x10" };

        [ObservableProperty]
        private int selectedSpeedIndex = 0;

        [ObservableProperty]
        Models.Region selectedRegion;

        [ObservableProperty]
        bool isRegionSelected;

        [ObservableProperty]
        bool isPlaying = false;

        [ObservableProperty]
        bool onMainView = true;

        [ObservableProperty]
        bool onChartView;

        [ObservableProperty]
        bool onVideoView;

        [ObservableProperty]
        ImageSource image;

        [ObservableProperty]
        MediaSource source;

        [ObservableProperty]
        double defaultSize = 100;

        [ObservableProperty]
        int mediaWidth;

        [ObservableProperty]
        int mediaHeight;

        [ObservableProperty]
        int mediaRotation;

        [ObservableProperty]
        int mediaLength;

        [ObservableProperty]
        double frameRate;

        public bool IsFlipped => MediaRotation == 90 || MediaRotation == -90;

        [ObservableProperty]
        double playerWidth;

        [ObservableProperty]
        double playerHeight;

        [ObservableProperty]
        double playerScale;

        [ObservableProperty]
        int videoWidth;

        [ObservableProperty]
        int videoHeight;

        public double RegionX
        {
            get => IsFlipped ? SelectedRegion?.Y ?? 0 : SelectedRegion?.X ?? 0;
            set
            {
                if (SelectedRegion != null)
                {
                    if (IsFlipped) SelectedRegion.Y = value;
                    else SelectedRegion.X = value;
                }
                OnPropertyChanged();
            }
        }

        public double RegionY
        {
            get => IsFlipped ? SelectedRegion?.X ?? 0 : SelectedRegion?.Y ?? 0;
            set
            {
                if (SelectedRegion != null)
                {
                    if (IsFlipped) SelectedRegion.X = value;
                    else SelectedRegion.Y = value;
                }
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        double progress;

        [ObservableProperty]
        int currentFrame = 0;

        [ObservableProperty]
        bool isRotated;

        [ObservableProperty]
        bool isAnalizing;

        [ObservableProperty]
        ISeries[] charts = new ISeries[] { };

        [ObservableProperty]
        bool back;

        bool stopAnalysis = false;

        Video_MP4 video;

        partial void OnSceneItemChanged(SceneItem sceneItem)
        {
            if (sceneItem != null)
            {
                SceneName = sceneItem.Name;
                var scenePath = sceneItem.ItemPath;
                Scene = Scene.Read(scenePath);
                if (Scene == null) return;
                VideoPath = Scene.VideoName;
                Utilities.GetMetadata(out var data, VideoPath);
                MediaRotation = (int)data[MetaType.VideoRotation];
                MediaLength = (int)data[MetaType.FrameCount];
                FrameRate = data[MetaType.FrameRate];
                //Debug.WriteLine($"[Debug]: {JsonConvert.SerializeObject(data)}");
                SetupSource();
                SetupVideo(); 
                UpdateChart();
            }
        }

        partial void OnSelectedRegionChanged(MicroVue.Models.Region region)
        {
            IsRegionSelected = SelectedRegion != null;
            OnPropertyChanged(nameof(RegionX));
            OnPropertyChanged(nameof(RegionY));
        }

        void SetupVideo(bool useImage = false)
        {
            if (!string.IsNullOrEmpty(VideoPath))
            {
                video = new Video_MP4(VideoPath);
                CurrentFrame = 0;
                VideoWidth = video?.Width ?? 0;
                VideoHeight = video?.Height ?? 0;
                IsRotated = MediaWidth > 0 && VideoWidth > 0 && VideoWidth == MediaHeight;
                //video.Count();
                //if (useImage) SetImage();
            }
        }

        void SetupSource()
        {
            if (!string.IsNullOrEmpty(VideoPath))
            {
                Source = MediaSource.FromFile(VideoPath);
            }
        }

        void SetImage()
        {
            if (video == null) return;
            var bitmap = video.GetFrame(CurrentFrame);
            if (bitmap == null) return;

            using (var ms = new MemoryStream())
            {
                bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
                ms.Position = 0;
                Image = ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
            }
        }

        void UpdateChart()
        {
            if (Scene?.Targets?.Count > 0)
            {
                var series = new List<ISeries>();
                foreach (var target in Scene.Targets)
                {
                    if (target?.IsBackground == false && target.Track?.RawPath?.Count > 0)
                    {
                        var fps = FrameRate > 0 ? FrameRate : 1;
                        var start = target.Track.RawPoints[0][DataDirection.Magnitude.ToString()];
                        var values = target.Track.RawPoints.Select(p => new ObservablePoint(p.Frame / fps, p[DataDirection.Magnitude.ToString()] - start)).ToArray();
                        series.Add(new LineSeries<ObservablePoint> { Values = values });
                    }
                }
                Charts = series.ToArray();
            }
        }

        partial void OnBackChanged(bool value)
        {
            if (value) _ = GoBack();
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        void Delete(MicroVue.Models.Region region)
        {
            if (Scene?.Regions != null && region != null)
            {
                Scene.Regions.Remove(region);
                SelectedRegion = null;
            }
        }

        [RelayCommand]
        void AddTarget()
        {
            if (Scene?.Regions == null) return;

            var id = 1;
            if (Scene.Regions.Count > 0)
            {
                while (Scene.Regions.Any(r => r.Id == id && !r.IsBackgound)) ++id;
            }

            var color = TargetColors[(id - 1) % TargetColors.Count];
            var width = IsFlipped ? MediaHeight : MediaWidth;
            var height = IsFlipped ? MediaWidth : MediaHeight;
            var target = new Models.Region(id, $"Target {id}", DefaultSize, width / 2, height / 2, false, color);
            Scene.Regions.Add(target);
            SelectedRegion = target;
            Scene.Save();
        }

        [RelayCommand]
        void AddBackground()
        {
            if (Scene?.Regions == null) return;

            var id = 1;
            if (Scene.Regions.Count > 0)
            {
                while (Scene.Regions.Any(r => r.Id == id && r.IsBackgound)) ++id;
            }

            var color = "White";
            var width = IsFlipped ? MediaHeight : MediaWidth;
            var height = IsFlipped ? MediaWidth : MediaHeight;
            var target = new Models.Region(id, $"Background {id}", DefaultSize, width / 2, height / 2, true, color);
            Scene.Regions.Add(target);
            SelectedRegion = target;
            Scene.Save();
        }

        [RelayCommand]
        void ShowRegions()
        {
            SelectedRegion = null;
            Scene.Save();
        }

        [RelayCommand]
        void Analyze()
        {
            if (video == null || !video.IsValid || Scene.Regions.Count == 0 || MediaLength == 0) return;

            Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine($"[Debug]: Starting analysis for {Scene.Regions.Count} region(s) in {MediaLength} frames.");
                    IsAnalizing = true;
                    stopAnalysis = false;
                    video.Reset();
                    if (!video.ReadFrame(out Image<Gray, byte> image)) return;
                    var targets = new ObservableCollection<Target>(Scene.Regions.Select(region => region.ToTarget()));
                    ImageAnalysis.StartFrame(image, targets);
                    var count = 1;
                    //image.Dispose();
                    while (video.ReadFrame(out image) && !stopAnalysis)
                    {
                        Debug.WriteLine($"[Debug]: - frame={count}, image={image}");
                        var found = ImageAnalysis.AnalyzeFrame(count, image, targets);
                        image.Dispose();
                        if (!found) break;
                        ++count;
                        Progress = (double)count / MediaLength;
                    }
                    Scene.Targets = targets;
                    Scene.Save();
                    UpdateChart();
                    Debug.WriteLine($"[Debug]: done, frame count = {count}.");
                    Debug.WriteLine($"[Debug]: {JsonConvert.SerializeObject(Scene.Targets[0].Track.RawPath, Formatting.Indented)}");
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"[Debug]: Error in analysis: {e}");
                }
                finally
                {
                    IsAnalizing = false;
                    stopAnalysis = false;
                    Progress = 0;
                }
            });
        }

        [RelayCommand]
        void Stop()
        {
            stopAnalysis = true;
        }
    }
}
