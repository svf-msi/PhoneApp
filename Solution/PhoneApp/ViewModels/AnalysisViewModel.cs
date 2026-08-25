using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
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
using MicroVue.Views;
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
        partial void OnOnVideoViewChanged(bool oldValue, bool newValue)
        {
            if (OnVideoView)
            {
                if (Scene?.Fois?.Count > 0 && SelectedFoi == null)
                {
                    SelectedFoi = Scene.Fois[0];
                }
            }
        }

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
            Initialize(sceneItem);
        }

        [ObservableProperty]
        Scene scene;

        [ObservableProperty]
        string sceneName;

        #endregion

        #region Video-related

        string videoPath;

        Video_MP4 video;

        [ObservableProperty]
        ImageSource image;

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

        public string Resolution => $"{VideoWidth}x{VideoHeight}";

        public bool IsFlipped => MediaRotation == 90 || MediaRotation == -90 || MediaRotation == 270 || MediaRotation == -270;

        [ObservableProperty]
        int mediaLength;

        [ObservableProperty]
        double duration;
        public void SetDuration(double value) => Duration = value * playbackRate / FrameRate;

        [ObservableProperty]
        double frameRate;
        partial void OnFrameRateChanged(double oldValue, double newValue)
        {
            if (FrameRate > 0 && Scene != null)
            {
                Scene.FrameRate = FrameRate;
                Scene.Save();
            }
        }

        double playbackRate = 30;

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
        double currentTime = 0;
        public void SetCurrentTime(double time) => CurrentTime = time * playbackRate / FrameRate;
        public double GetCurrentTime() => CurrentTime * FrameRate / playbackRate;

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
            if (IsRegionSelected)
            {
                if (IsRegionsShown) IsRegionsShown = false;
                if (IsRangeShown) IsRangeShown = false;
                if (IsCalibrationShown) IsCalibrationShown = false;
            }
            else
            {
                if (!IsRegionsShown) IsRegionsShown = true;
                if (IsRangeShown) IsRangeShown = false;
                if (IsCalibrationShown) IsCalibrationShown = false;
            }
            OnPropertyChanged(nameof(RegionX));
            OnPropertyChanged(nameof(RegionY));
        }

        [ObservableProperty]
        bool isRegionsShown = true;

        [ObservableProperty]
        bool isRangeShown;

        [ObservableProperty]
        bool isCalibrationShown;

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

        public double StartTime { get => Scene?.StartTime ?? 0; set { Scene.StartTime = value; OnPropertyChanged(); } }

        public double EndTime { get => Scene?.EndTime ?? 0; set { Scene.EndTime = value; OnPropertyChanged(); } }

        [ObservableProperty]
        bool isAnalizing;

        [ObservableProperty]
        double progress;

        [ObservableProperty]
        string analysisResults;

        bool stopAnalysis = false;

        public List<Models.DistanceUnits> Units => new List<Models.DistanceUnits>
        {
            Models.DistanceUnits.inches, Models.DistanceUnits.feet, Models.DistanceUnits.cm, Models.DistanceUnits.meters
        };

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

        #region FOI-related

        [ObservableProperty]
        Foi selectedFoi;
        partial void OnSelectedFoiChanged(Foi oldValue, Foi newValue)
        {
            OnPropertyChanged(nameof(SelectedFoiName));
            OnPropertyChanged(nameof(SelectedFoiFrequency));
            OnPropertyChanged(nameof(SelectedFoiMagnification));
            SetFoiSource(SelectedFoi);
        }

        [ObservableProperty]
        bool isFoiModified;

        [ObservableProperty]
        bool isModifyingFoi;

        [ObservableProperty]
        double foiProgress;

        public string SelectedFoiName => SelectedFoi?.Name;

        public double SelectedFoiFrequency => SelectedFoi?.Frequency ?? 0;

        public double SelectedFoiMagnification { get => SelectedFoi?.Magnification ?? 0; set { SelectedFoi.Magnification = (int)value; OnPropertyChanged(); } }

        [ObservableProperty]
        MediaSource foiVideoSource;

        #endregion

        #endregion

        #region Setup

        void Initialize(SceneItem sceneItem)
        {
            if (sceneItem != null && sceneItem.Type == ItemType.SceneFolder)
            {
                Scene = Scene.Open(sceneItem.ItemPath);
                SceneName = Scene.Name;
                if (Scene == null) return;
                videoPath = $"{Scene.CurrentFolder}/{Scene.VideoName}";
                //Debug.WriteLine($"[Debug]: video = {videoPath}, {Scene.CurrentFolder}, {Scene.VideoName}");
                SetupVideo();

                if (Scene.ValidParams)
                {
                    MediaRotation = Scene.Rotation;
                    MediaLength = Scene.FrameCount;
                    FrameRate = Scene.FrameRate;
                }
                else
                {
                    Utilities.GetMetadata(out var data, videoPath);
                    Scene.Rotation = MediaRotation = (int)data[MetaType.VideoRotation];
                    Scene.FrameCount = MediaLength = (int)data[MetaType.FrameCount];
                    Scene.FrameRate = FrameRate = data[MetaType.FrameRate];
                    Scene.VideoWidth = VideoWidth;
                    Scene.VideoHeight = VideoHeight;
                    Scene.ValidParams = true;
                    Scene.Save();
                    //Debug.WriteLine($"[Debug]: {JsonConvert.SerializeObject(data)}");
                }

                Scene.Calibration.MediaWidth = IsFlipped ? VideoHeight : VideoWidth;
                Scene.Calibration.MediaHeight = IsFlipped ? VideoWidth : VideoHeight;

                SetupSource();
                UpdateChart();
            }
        }

        void SetupVideo(bool useImage = false)
        {
            if (!string.IsNullOrEmpty(videoPath))
            {
                video = new Video_MP4(videoPath);
                VideoWidth = video?.Width ?? 0;
                VideoHeight = video?.Height ?? 0;
                //if (useImage) SetImage();
            }
        }

        void SetupSource()
        {
            if (!string.IsNullOrEmpty(videoPath))
            {
                Source = MediaSource.FromFile(videoPath);
            }
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(StartTime));
            OnPropertyChanged(nameof(EndTime));
        }

        public double GetStartPosition() => StartTime * FrameRate / playbackRate;

        #endregion

        #region Charts

        public void UpdateChart()
        {
            //Debug.WriteLine($"[Debug]: Update chart");
            if (Scene?.Targets?.Count > 0)
            {
                var direction = DataDirection;
                if (direction != DataDirection.Magnitude && IsFlipped)
                {
                    direction = direction == DataDirection.X ? DataDirection.Y : DataDirection.X;
                }
                var series = new List<ISeries>();
                MinX = MaxX = 0;
                var scale = Scene.CalibrationScale;
                var units = Scene.DistanceUnits;
                var label = units == Models.DistanceUnits.meters || units == Models.DistanceUnits.cm ? "mm" : "mil";
                if (units == Models.DistanceUnits.inches) scale *= 1e3;
                if (units == Models.DistanceUnits.feet) scale *= 1.2e4;
                if (units == Models.DistanceUnits.cm) scale *= 1e2;
                if (units == Models.DistanceUnits.meters) scale *= 1e3;

                foreach (var target in Scene.Targets)
                {
                    //Debug.WriteLine($"[Debug]: target={target.Name}, back={target.IsBackground}, points={target.Track.Path?.Count()}");
                    if (target?.IsBackground == false && target.Track?.RawPath?.Count > 0)
                    {
                        var fps = FrameRate > 0 ? FrameRate : 1;
                        List<ObservablePoint> values = new List<ObservablePoint>();
                        if (!IsSpectrum)
                        {
                            var start = target.Track.RawPoints[0][direction.ToString()];
                            values = target.Track.RawPoints.Select(p => new ObservablePoint(p.Frame / fps, scale * (p[direction.ToString()] - start))).ToList();
                        }
                        else
                        {
                            var track = target.Track.RawPoints.Select(p => (double)p[direction.ToString()]).ToArray();
                            var mean = track.Average();
                            var waveform = track.Select(p => scale * (p - mean)).ToArray();
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
                AxisYTitle = (DataDirection == DataDirection.X ? "X displacement" : DataDirection == DataDirection.Y ? "Y displacement" : "Total displacement");
                if (Scene.ScaleCalibrated) AxisYTitle += $"({label})";
                Lines = series.ToArray();
                UpdateSections();
            }
        }

        void UpdateSections()
        {
            var foiCollection = new SectionsCollection();
            if (Scene?.Fois?.Count > 0 && IsSpectrum)
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
            var bitmap = video.GetFrame(0);
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
                var foi = new Foi { Id = id, Name = $"FOI {id}", SceneName = SceneName, Frequency = frequency };
                Scene.Fois.Add(foi);
            }
        }

        void SetFoiSource(Foi foi)
        {
            if (!string.IsNullOrEmpty(foi?.VideoFile))
            {
                FoiVideoSource = MediaSource.FromFile(foi.GetFullPath(foi?.VideoFile));
            }
        }

        void ShowAnalysisResutls()
        {
            var analysisView = new AnalysisView();
            analysisView.BindingContext = this;

            Task.Run(async () =>
            {
                await Shell.Current.ShowPopupAsync(analysisView);

            });
        }

        #endregion

        #region Commands

        #region Main

        [RelayCommand]
        void ShowRange()
        {
            IsRegionsShown = false;
            IsRangeShown = true;
        }

        [RelayCommand]
        void ShowCalibration()
        {
            IsRegionsShown = false;
            IsCalibrationShown = true;
        }

        [RelayCommand]
        void SetTime(object parameter)
        {
            //Debug.WriteLine($"[Debug]: set time {parameter} to {CurrentTime}");
            if (parameter is string time)
            {
                if (time == "Start")
                {
                    StartTime = CurrentTime;
                }
                else if (time == "End")
                {
                    EndTime = CurrentTime;
                }
            }
        }

        [RelayCommand]
        void GoToTime(object parameter)
        {
            if (parameter is string time)
            {
                if (time == "Start")
                {
                    CurrentTime = StartTime;
                }
                else if (time == "End")
                {
                    CurrentTime = EndTime;
                }
            }
        }


        [RelayCommand]
        void Analyze()
        {
            if (video == null || !video.IsValid || Scene.Regions.Count == 0 || MediaLength == 0) return;

            Task.Run(async () =>
            {
                try
                {
                    IsAnalizing = true;
                    stopAnalysis = false;

                    var startFrame = Math.Max(0, (int)Math.Round(StartTime * FrameRate));
                    var endFrame = Math.Min((int)Math.Round(EndTime * FrameRate), MediaLength - 1);
                    endFrame = endFrame > startFrame ? endFrame : MediaLength - 1;
                    var length = endFrame - startFrame + 1;

                    //Debug.WriteLine($"[Debug]: Starting analysis for {Scene.Regions.Count} region(s) from {startFrame} to {endFrame} frames.");

                    video.Reset();

                    var count = 0;

                    Image<Gray, byte> image = null;
                    var targets = new ObservableCollection<Target>(Scene.Regions.Select(region => region.ToTarget(startFrame)));

                    while (count <= startFrame)
                    {
                        image?.Dispose();
                        if (!video.ReadFrame(out image)) return;
                        ++count;
                    }
                    ImageAnalysis.StartFrame(image, targets, startFrame);

                    image.Dispose();
                    while (video.ReadFrame(out image) && !stopAnalysis)
                    {
                        //Debug.WriteLine($"[Debug]: - frame={count}, image={image}");
                        var found = ImageAnalysis.AnalyzeFrame(count, image, targets);
                        image?.Dispose();
                        if (!found) break;

                        ++count;
                        Progress = (double)(count - startFrame) / length;
                        if (count > endFrame) break;
                    }

                    var completed = true;
                    foreach (var target in targets)
                    {
                        target.Completion = (int)Math.Round((double)(target.EndFrame - target.StartFrame + 1) / length * 100);
                        if (target.Completion < 100) completed = false;
                    }

                    Scene.Targets = targets;
                    ImageAnalysis.ProcessBackground(Scene.BackgroundAnalysis, targets);
                    Scene.BackgroundAnalysis.TransformPoins = null;

                    // Reset filtered videos
                    if (Scene.Fois?.Count > 0)
                    {
                        foreach (var foi in Scene.Fois)
                        {
                            foi?.Reset();
                        }
                    }

                    Scene.Save();
                    UpdateChart();
                    //Debug.WriteLine($"[Debug]: done, frame count = {count-startFrame} out of {MediaLength}, {startFrame}-{endFrame} requested.");
                    //Debug.WriteLine($"[Debug]: {JsonConvert.SerializeObject(Scene.Targets[0].Track.RawPath, Formatting.Indented)}");

                    if (completed)
                    {
                        AnalysisResults = "Success!";
                    }
                    else
                    {
                        AnalysisResults = "Incomplete!";
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"[Debug]: Error in analysis: {e}");
                    AnalysisResults = "Error!";
                }
                finally
                {
                    IsAnalizing = false;
                    stopAnalysis = false;
                    Progress = 0;

                    ShowAnalysisResutls();
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
                UpdateChart();
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
            var width = VideoWidth; 
            var height = VideoHeight; 
            var target = new Models.Region(id, $"Target {id}", DefaultSize, width / 2, height / 2, false, color);
            //Debug.WriteLine($"[Debug]: add target {Utils.ToString(target)}");
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
            var width = VideoWidth; 
            var height = VideoHeight; 
            var target = new Models.Region(id, $"Anchor {id}", DefaultSize, width / 2, height / 2, true, color);
            //Debug.WriteLine($"[Debug]: add background {Utils.ToString(target)}");
            Scene.Regions.Add(target);
            SelectedRegion = target;
            Scene.Save();
        }

        [RelayCommand]
        void ShowRegions()
        {
            SelectedRegion = null;
            Scene.Save();
            IsRegionsShown = true;
            IsRangeShown = false;
            IsCalibrationShown = false;
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
            foi?.Remove();
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
                        foi.IsSaving = false;
                        video.Reset();
                        video.Transforms = Scene.BackgroundAnalysis.TransformPoins;
                        video.UseTransform = !Scene.BackgroundAnalysis.Empty;
                        foi.Cts = new CancellationTokenSource();
                        var startFrame = Math.Max(0, (int)Math.Round(StartTime * FrameRate));
                        var endFrame = Math.Min((int)Math.Round(EndTime * FrameRate), MediaLength - 1);
                        endFrame = endFrame > startFrame ? endFrame : MediaLength - 1;
                        if (ImageAnalysis.FilterFrequency(foi.Frequency, FrameRate > 0 ? FrameRate : 1, startFrame, endFrame, video,
                            out var real, out var imag, out var average, foi.Cts.Token, (double p) => foi.Progress = p))
                        {
                            foi.RealImage = real; 
                            foi.ImagImage = imag; 
                            foi.AverageImage = average; 
                            
                            foi.MakeVideo((double p) => foi.Progress = p);

                            if (foi.Cts.Token.IsCancellationRequested)
                            {
                                foi.IsNotProcessed = true;
                                foi.IsProcessing = false;
                                foi.IsReady = false;
                            }
                            else
                            {
                                foi.IsNotProcessed = false;
                                foi.IsProcessing = false;
                                foi.IsReady = true;
                            }

                            foi.IsSaving = true;
                            Scene.Save();
                            foi.IsSaving = false;
                        }
                        else
                        {
                            foi.IsNotProcessed = true;
                            foi.IsProcessing = false;
                            foi.IsReady = false;
                            foi.IsSaving = false;
                        }

                        //Debug.WriteLine(Utilities.ListFolderContents(App.FoiDataFolder));
                        //Debug.WriteLine(Utilities.ListFolderContents(App.FoiVideoFolder));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Error in filtering: {e}");
                        foi.IsNotProcessed = true;
                        foi.IsProcessing = false;
                        foi.IsReady = false;
                        foi.IsSaving = false;
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
                SelectedFoi = foi;
                OnVideoView = true;
            }
        }

        [RelayCommand]
        void ModifyFoi(Foi foi)
        {
            if (foi != null)
            {
                IsFoiModified = true;
                SelectedFoi = foi;
            }
        }

        [RelayCommand]
        void ReturnFromFoi()
        {
            IsFoiModified = false;
        }

        [RelayCommand]
        void RemakeFoiVideo()
        {
            if (SelectedFoi != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        FoiProgress = 0;
                        IsModifyingFoi = true;
                        SelectedFoi.Cts = new CancellationTokenSource();
                        SelectedFoi.MakeVideo((double p) => FoiProgress = p);
                        if (!SelectedFoi.Cts.Token.IsCancellationRequested)
                        {
                            Scene.Save();
                            SetFoiSource(SelectedFoi);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Error in remaking foi: {e}");
                    }
                    finally
                    {
                        IsModifyingFoi = false;
                    }
                });
            }
        }

        [RelayCommand]
        void StopRemakingFoi()
        {
            SelectedFoi?.Cts?.Cancel();
        }

        #endregion

        #endregion
    }
}
