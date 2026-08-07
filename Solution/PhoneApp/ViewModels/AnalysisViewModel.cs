using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emgu.CV;
using Emgu.CV.Structure;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using MicroVue.Models;
using Newtonsoft.Json;
using SkiaSharp;
using StandardLib;
using Syncfusion.Maui.Toolkit.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace MicroVue.ViewModels
{
    [QueryProperty(nameof(SceneItem), "SceneItem")]
    public partial class AnalysisViewModel : ObservableObject
    {
        #region Fields and Properties

        #region Main

        [ObservableProperty]
        bool onMainView = true;

        [ObservableProperty]
        bool onChartView;

        [ObservableProperty]
        bool onVideoView;

        [ObservableProperty]
        bool back;
        partial void OnBackChanged(bool value)
        {
            if (value) _ = GoBack();
        }

        #endregion

        #region Scene-related

        [ObservableProperty]
        SceneItem sceneItem = new SceneItem();
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

        [ObservableProperty]
        Scene scene;

        [ObservableProperty]
        string sceneName;

        #endregion

        #region Video-related

        Video_MP4 video;

        [ObservableProperty]
        ImageSource image;

        [ObservableProperty]
        string videoPath;

        [ObservableProperty]
        int videoWidth;

        [ObservableProperty]
        int videoHeight;

        [ObservableProperty]
        int mediaWidth;

        [ObservableProperty]
        int mediaHeight;

        [ObservableProperty]
        int mediaRotation;

        public bool IsFlipped => MediaRotation == 90 || MediaRotation == -90;

        [ObservableProperty]
        int mediaLength;

        [ObservableProperty]
        double frameRate;

        [ObservableProperty]
        bool isRotated;

        #region Player-related

        [ObservableProperty]
        MediaSource source;

        [ObservableProperty]
        bool isPlaying = false;

        [ObservableProperty]
        private ObservableCollection<string> speeds = new() { "x1", "x2", "x10" };

        [ObservableProperty]
        private int selectedSpeedIndex = 0;

        [ObservableProperty]
        double playerWidth;

        [ObservableProperty]
        double playerHeight;

        [ObservableProperty]
        double playerScale;

        [ObservableProperty]
        int currentFrame = 0;

        #endregion

        #endregion

        #region Target-related

        [ObservableProperty]
        List<string> targetColors = new List<string> { "Red", "Green", "Blue", "Yellow", "Teal", "Purple" };

        [ObservableProperty]
        double defaultSize = 100;

        [ObservableProperty]
        Models.Region selectedRegion;
        partial void OnSelectedRegionChanged(Models.Region? oldRegion, Models.Region newRegion)
        {
            IsRegionSelected = SelectedRegion != null;
            OnPropertyChanged(nameof(RegionX));
            OnPropertyChanged(nameof(RegionY));
        }

        [ObservableProperty]
        bool isRegionSelected;

        public double RegionX
        {
            get
            {
                if (SelectedRegion != null)
                {
                    switch (MediaRotation)
                    {
                        case -90:
                        case 270:
                            return SelectedRegion.Y;
                        case -180:
                        case 180:
                            return MediaWidth - SelectedRegion.X;
                        case -270:
                        case 90:
                            return MediaWidth - SelectedRegion.Y;
                        default:
                            return SelectedRegion.X;
                    }
                }
                return 0;
            }
            set
            {
                if (SelectedRegion != null)
                {
                    switch (MediaRotation)
                    {
                        case -90:
                        case 270:
                            SelectedRegion.Y = value;
                            break;
                        case -180:
                        case 180:
                            SelectedRegion.X = MediaWidth - value;
                            break;
                        case -270:
                        case 90:
                            SelectedRegion.Y = MediaWidth - value;
                            break;
                        default:
                            SelectedRegion.X = value;
                            break;
                    }
                }
                OnPropertyChanged();
            }
        }

        public double RegionY
        {
            get
            {
                if (SelectedRegion != null)
                {
                    switch (MediaRotation)
                    {
                        case -90:
                        case 270:
                            return MediaHeight - SelectedRegion.X;
                        case -180:
                        case 180:
                            return MediaHeight - SelectedRegion.Y;
                        case -270:
                        case 90:
                            return SelectedRegion.X;
                        default:
                            return SelectedRegion.Y;
                    }
                }
                return 0;
            }
            set
            {
                if (SelectedRegion != null)
                {
                    switch (MediaRotation)
                    {
                        case -90:
                        case 270:
                            SelectedRegion.X = MediaHeight - value;
                            break;
                        case -180:
                        case 180:
                            SelectedRegion.Y = MediaHeight - value;
                            break;
                        case -270:
                        case 90:
                            SelectedRegion.X = value;
                            break;
                        default:
                            SelectedRegion.Y = value;
                            break;
                    }
                }
                OnPropertyChanged();
            }
        }

        #endregion

        #region Analysis-related

        [ObservableProperty]
        bool isAnalizing;

        [ObservableProperty]
        double progress;

        bool stopAnalysis = false;

        #endregion

        #region Chart-related

        [ObservableProperty]
        string axisXTitle = "Time(secs)";

        [ObservableProperty]
        string axisYTitle = "Displacement";

        [ObservableProperty]
        double minX;

        [ObservableProperty]
        double maxX;

        [ObservableProperty]
        ISeries[] lines = new ISeries[] { };

        [ObservableProperty]
        SectionsCollection sections = new SectionsCollection();

        [ObservableProperty]
        LiveChartsCore.Measure.ZoomAndPanMode zoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;

        public DrawMarginFrame DrawMarginFrame => new()
        {
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("3c3c3c"), 1)
        };

        [ObservableProperty]
        bool isSpectrum = true;
        partial void OnIsSpectrumChanged(bool oldValue, bool newValue)
        {
            UpdateChart();
        }

        [ObservableProperty]
        DataDirection dataDirection = DataDirection.Magnitude;
        partial void OnDataDirectionChanged(DataDirection oldValue, DataDirection newValue)
        {
            UpdateChart();
        }

        double binSize = 1;

        #endregion

        #endregion

        #region Setup

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

        #endregion

        #region Charts

        void UpdateChart()
        {
            //Debug.WriteLine($"[Debug]: Update chart");
            if (Scene?.Targets?.Count > 0)
            {
                var direction = DataDirection;
                var series = new List<ISeries>();
                MinX = MaxX = 0;
                foreach (var target in Scene.Targets)
                {
                    if (target?.IsBackground == false && target.Track?.RawPath?.Count > 0)
                    {
                        var fps = FrameRate > 0 ? FrameRate : 1;
                        List<ObservablePoint> values = new List<ObservablePoint>();
                        if (!IsSpectrum)
                        {
                            var start = target.Track.RawPoints[0][direction.ToString()];
                            values = target.Track.RawPoints.Select(p => new ObservablePoint(p.Frame / fps, p[direction.ToString()] - start)).ToList();
                        }
                        else
                        {
                            var track = target.Track.RawPoints.Select(p => (double)p[direction.ToString()]).ToArray();
                            var mean = track.Average();
                            var waveform = track.Select(p => p - mean).ToArray();
                            var spectrum = FftAnalysis.Emgu(waveform, WindowType.Hann);
                            var span = spectrum.GetLength(1);
                            binSize = fps / span;
                            //values.Add(new ObservablePoint(0, 0));
                            for (int i = 5; i < span / 2; ++i)
                            {
                                var disp = FftAnalysis.Complex(spectrum, 0, 2 * i - 1);
                                values.Add(new ObservablePoint(i * fps / span, disp.Magnitude));
                            }

                            MinX = 0;
                            MaxX = fps / 2;
                        }
                        series.Add(new LineSeries<ObservablePoint>
                        {
                            Values = values,
                            Stroke = new SolidColorPaint(Utilities.ConvertToSKColor(target.ColorText)) { StrokeThickness = 2 },
                            Fill = null,
                            GeometryFill = null,
                            GeometryStroke = null,
                            GeometrySize = 0,
                            LineSmoothness = 0
                        });
                    }
                }
                AxisXTitle = IsSpectrum ? "Frequency(Hz)" : "Time(secs)";
                AxisYTitle = DataDirection == DataDirection.X ? "X displacement" : DataDirection == DataDirection.Y ? "Y displacement" : "Total displacement";
                Lines = series.ToArray();
                UpdateSections();
            }
        }

        void UpdateSections()
        {
            var foiCollection = new SectionsCollection();
            if (Scene?.Fois?.Count > 0)
            {
                foreach (var foi in Scene.Fois)
                {
                    foiCollection.Add(new XamlRectangularSection { Xi = foi.Frequency - binSize, Xj = foi.Frequency + binSize, Fill = new SolidColorPaint(SKColors.DarkMagenta) });
                }
            }
            Sections = foiCollection;
        }

        #endregion

        #region Miscellaneous

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

        void AddFoi(double frequency)
        {
            if (Scene != null)
            {
                if (Scene.Fois == null) Scene.Fois = new ObservableCollection<Foi>();
                var id = Scene.Fois.Count == 0 ? 1 : Scene.Fois.Last().Id + 1;
                var foi = new Foi { Id = id, Name = $"FOI {id}", Frequency = frequency };
                Scene.Fois.Add(foi);
            }
        }

        #endregion

        #region Commands

        #region Main

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

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        #endregion

        #region Target-related

        [RelayCommand]
        void DeleteRegion(MicroVue.Models.Region region)
        {
            if (Scene?.Regions != null && region != null)
            {
                Scene.Regions.Remove(region);
                SelectedRegion = null;
            }
            if (Scene?.Targets != null && region != null)
            {
                var target = Scene?.Targets.FirstOrDefault(target => target.Name == region.Name);
                if (target != null) Scene.Targets.Remove(target);
            }
        }

        [RelayCommand]
        void ViewRegion(MicroVue.Models.Region region)
        {
            //Debug.WriteLine($"[Debug]: select {region?.Name} region");
            SelectedRegion = region;
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

        #endregion

        #region Foi-related

        [RelayCommand]
        void SelectPeakFrequency()
        {
            if (IsSpectrum && Lines?.Length > 0)
            {
                var peak = 0.0;
                var peakFrequency = 0.0;
                foreach (var chart in Lines)
                {
                    foreach (var value in chart.Values)
                    {
                        if (value is ObservablePoint point && point.X >= MinX && point.X <= MaxX && point.Y > peak)
                        {
                            peak = point.Y ?? 0;
                            peakFrequency = point.X ?? 0;
                        }
                    }
                }
                AddFoi(peakFrequency);
                UpdateSections();
                Scene?.Save();
            }
        }

        [RelayCommand]
        void DeleteFoi(Foi foi)
        {
            Scene?.Fois?.Remove(foi);
            UpdateSections();
            Scene?.Save();
        }

        [RelayCommand]
        void ProcessFoi(Foi foi)
        {
            if (foi != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        foi.IsNotProcessed = false;
                        foi.IsProcessing = true;
                        foi.IsReady = false;
                        foi.Cts = new CancellationTokenSource();
                        if (ImageAnalysis.FilterFrequency(foi.Frequency, FrameRate, MediaLength, video,
                            out var real, out var imag, out var average, foi.Cts.Token, (double p) => foi.Progress = p))
                        {
                            foi.IsNotProcessed = false;
                            foi.IsProcessing = false;
                            foi.IsReady = true;
                        }
                        else
                        {
                            foi.IsNotProcessed = true;
                            foi.IsProcessing = false;
                            foi.IsReady = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Error in filtering: {e}");
                        foi.IsNotProcessed = true;
                        foi.IsProcessing = false;
                        foi.IsReady = false;
                    }
                });
            }
        }

        [RelayCommand]
        void StopProcessFoi(Foi foi)
        {
            foi.Cts?.Cancel();
        }

        [RelayCommand]
        void ViewFoiVideo(Foi foi)
        {
            if (foi != null)
            {
                
            }
        }

        #endregion

        #endregion
    }
}
