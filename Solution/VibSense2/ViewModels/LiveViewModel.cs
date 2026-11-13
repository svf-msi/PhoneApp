using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VibSense.Models;

namespace VibSense.ViewModels
{
    public partial class LiveViewModel : ObservableObject
    {
        #region Fields and Properties

        #region Common

        [ObservableProperty]
        bool showChart = true;

        [ObservableProperty]
        bool showSaving = false;

        [ObservableProperty]
        bool showSettings = false;

        [ObservableProperty]
        bool showHelp = false;

        [ObservableProperty]
        string helpText = "Help text";

        [ObservableProperty]
        bool useNew = true;

        [ObservableProperty]
        bool useExisting = false;

        [ObservableProperty]
        int existingSetIndex = 0;
        partial void OnExistingSetIndexChanged(int value)
        {
            if (ExistingSetIndex >= 0 && ExistingSetIndex < Projects.Count)
            {
                selectedSet = Projects[ExistingSetIndex];
                var project = Project.Read(selectedSet?.ItemPath);
                NewChartName = $"Chart {(project?.DataCharts?.Count ?? 0) + 1}";
            }
            else
            {
                selectedSet = null;
                NewChartName = $"Chart 1";
            }
        }

        [ObservableProperty]
        string newSetName = "";

        [ObservableProperty]
        string newChartName = "";

        [ObservableProperty]
        ObservableCollection<ProjectItem> projects = new ObservableCollection<ProjectItem>();

        [ObservableProperty]
        ObservableCollection<string> existingSets = new ObservableCollection<string>();

        ProjectItem selectedSet;

        #endregion

        #region Accelerometer/Data

        int refreshCount = 0;
        DateTime start;
        AccelerometerService accelerometerService;
        OrientationService orientationService;

        [ObservableProperty]
        bool isRecording = false;

        [ObservableProperty]
        double minDuration = 1;
        partial void OnMinDurationChanged(double value)
        {
            Preferences.Set(App.SettingMinDuration, MinDuration);
        }

        [ObservableProperty]
        double maxDuration = 10;
        partial void OnMaxDurationChanged(double value)
        {
            Preferences.Set(App.SettingMaxDuration, MaxDuration);
        }

        [ObservableProperty]
        bool isMetric = false;

        [ObservableProperty]
        bool isImperial = true;
        partial void OnIsImperialChanged(bool value)
        {
            var units = value ? 0 : 1;
            Preferences.Set(App.SettingUnits, units);
            UpdateChart();
        }

        [ObservableProperty]
        Coordinates coordinates;
        partial void OnCoordinatesChanged(Coordinates value)
        {
            Preferences.Set(App.SettingCoordinates, (int)Coordinates);
            UpdateLabels();
            UpdateChart();
        }

        [ObservableProperty]
        bool internalCoordinates = true;
        partial void OnInternalCoordinatesChanged(bool value)
        {
            if (value) Coordinates = Coordinates.Internal;
        }

        [ObservableProperty]
        bool globalCoordinates = false;
        partial void OnGlobalCoordinatesChanged(bool value)
        {
            if (value) Coordinates = Coordinates.Global;
        }

        [ObservableProperty]
        string labelX = "X";

        [ObservableProperty]
        string labelY = "Y";

        [ObservableProperty]
        string labelZ = "Z";

        [ObservableProperty]
        bool buttonLock = false;

        public double SampleRate { get; set; } = 400;

        public List<Vector3> Data { get; set; } = new List<Vector3>();

        public Quaternion Quaternion { get; protected set; }

        [ObservableProperty]
        int count = 0;

        [ObservableProperty]
        bool hasData = false;

        [ObservableProperty]
        double roll;

        [ObservableProperty]
        double pitch;

        [ObservableProperty]
        double yaw;

        #endregion

        #region Chart-related

        [ObservableProperty]
        public Axis[] xAxes;

        public Axis XAxis { get; set; }

        [ObservableProperty]
        public Axis[] yAxes;

        public Axis YAxis { get; set; }

        [ObservableProperty]
        ISeries[] lines;

        [ObservableProperty]
        LiveChartsCore.Measure.ZoomAndPanMode zoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;

        public DrawMarginFrame DrawMarginFrame => new()
        {
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("3c3c3c"), 1)
        };

        [ObservableProperty]
        Direction direction = Direction.Magnitude;
        partial void OnDirectionChanged(Direction value)
        {
            Preferences.Set(App.SettingDirection, (int)Direction);
            UpdateChart();
        }

        [ObservableProperty]
        DataType dataType = DataType.Acceleration;
        partial void OnDataTypeChanged(DataType value)
        {
            Preferences.Set(App.SettingType, (int)DataType);
            UpdateChart();
        }
        [ObservableProperty]
        bool directionAll = false;
        partial void OnDirectionAllChanged(bool value)
        {
            if (value) Direction = Direction.Magnitude;
        }

        [ObservableProperty]
        bool directionX = false;
        partial void OnDirectionXChanged(bool value)
        {
            if (value) Direction = Direction.X;
        }

        [ObservableProperty]
        bool directionY = false;
        partial void OnDirectionYChanged(bool value)
        {
            if (value) Direction = Direction.Y;
        }

        [ObservableProperty]
        bool directionZ = false;
        partial void OnDirectionZChanged(bool value)
        {
            if (value) Direction = Direction.Z;
        }

        [ObservableProperty]
        bool typeDisplacement = false;
        partial void OnTypeDisplacementChanged(bool value)
        {
            if (value) DataType = DataType.Displacement;
        }

        [ObservableProperty]
        bool typeVelocity = false;
        partial void OnTypeVelocityChanged(bool value)
        {
            if (value) DataType = DataType.Velocity;
        }

        [ObservableProperty]
        bool typeAcceleration = false;
        partial void OnTypeAccelerationChanged(bool value)
        {
            if (value) DataType = DataType.Acceleration;
        }

        [ObservableProperty]
        ChartType chartType;
        partial void OnChartTypeChanged(ChartType value)
        {
            Preferences.Set(App.SettingChartType, (int)ChartType);
            InitializeChart();
            UpdateChart();
        }

        [ObservableProperty]
        bool linearChart = true;
        partial void OnLinearChartChanged(bool value)
        {
            if (value) ChartType = ChartType.Linear;
        }

        [ObservableProperty]
        bool logChart = false;
        partial void OnLogChartChanged(bool value)
        {
            if (value) ChartType = ChartType.Logarithmic;
        }

        #endregion

        #endregion

        #region Setup

        public LiveViewModel()
        {
            accelerometerService = new AccelerometerService();
            accelerometerService.AccelerometerChanged += AccelerometerChanged;

            orientationService = new OrientationService();
            orientationService.OnOrientationChanged += OrientationChanged;

            ReadHelp();
            InitializeSettings();
            InitializeChart();
            RefreshProjects();
        }

        void InitializeChart()
        {
            XAxis = new Axis
            {
                Name = "Frequency(Hz)",
                TextSize = 18,
                NamePadding = new LiveChartsCore.Drawing.Padding(0, -10, 0, 5),
                NamePaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                LabelsPaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
                SubseparatorsPaint = new SolidColorPaint(new SKColor(160, 160, 160)) { StrokeThickness = 0.5f },
                TicksPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
            };
            XAxes = new Axis[] { XAxis };

            if (ChartType == ChartType.Logarithmic)
                YAxis = new LogarithmicAxis(10)
                {
                    Name = DataType == DataType.Displacement ? "Displacement(pk-pk)" :
                        DataType == DataType.Velocity ? "Velocity(rms)" : "Acceleration(G pk)",
                    TextSize = 18,
                    NamePadding = new LiveChartsCore.Drawing.Padding(0, 5, 0, -10),
                    NamePaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                    LabelsPaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
                    SubseparatorsPaint = new SolidColorPaint(new SKColor(160, 160, 160)) { StrokeThickness = 0.5f },
                    TicksPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
                    SubseparatorsCount = 9,
                    Labeler = (value) => Math.Pow(10, value).ToString("G2")
                };
            else
                YAxis = new Axis
                {
                    Name = DataType == DataType.Displacement ? "Displacement(pk-pk)" :
                        DataType == DataType.Velocity ? "Velocity(rms)" : "Acceleration(G pk)",
                    TextSize = 18,
                    NamePadding = new LiveChartsCore.Drawing.Padding(0, 5, 0, -10),
                    NamePaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                    LabelsPaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
                    SubseparatorsPaint = new SolidColorPaint(new SKColor(160, 160, 160)) { StrokeThickness = 0.5f },
                    TicksPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
                    Labeler = (value) => value.ToString("G2")
                };
            YAxes = new Axis[] { YAxis };
            Lines = new ISeries[] { };
        }

        void InitializeSettings()
        {
            ChangeDirection((Direction)Preferences.Get(App.SettingDirection, 0));
            ChangeType((DataType)Preferences.Get(App.SettingType, 0));

            MinDuration = Preferences.Get(App.SettingMinDuration, 1.0);
            MaxDuration = Preferences.Get(App.SettingMaxDuration, 10.0);

            var units = Preferences.Get(App.SettingUnits, 0);
            IsMetric = units != 0;
            IsImperial = units == 0;

            var chartType = Preferences.Get(App.SettingChartType, 0);
            LinearChart = chartType == 0;
            LogChart = chartType != 0;

            var coords = Preferences.Get(App.SettingCoordinates, 0);
            InternalCoordinates = coords == 0;
            GlobalCoordinates = coords != 0;
            UpdateLabels();
        }

        void ChangeDirection(Direction dir)
        {
            Direction = dir;
            DirectionAll = Direction == Direction.Magnitude;
            DirectionX = Direction == Direction.X;
            DirectionY = Direction == Direction.Y;
            DirectionZ = Direction == Direction.Z;
        }

        void ChangeType(DataType type)
        {
            DataType = type;
            TypeDisplacement = DataType == DataType.Displacement;
            TypeVelocity = DataType == DataType.Velocity;
            TypeAcceleration = DataType == DataType.Acceleration;
            UpdateChart();
        }

        void ReadHelp()
        {
            Task.Run(async () =>
            {
                try
                {
                    using Stream stream = await FileSystem.Current.OpenAppPackageFileAsync("help.txt");
                    using StreamReader reader = new StreamReader(stream);

                    HelpText = await reader.ReadToEndAsync();
                }
                catch (FileNotFoundException)
                {
                    HelpText = "Help file not found.";
                }
            });
        }

        void UpdateLabels()
        {
            LabelX = InternalCoordinates ? "X" : "N";
            LabelY = InternalCoordinates ? "Y" : "E";
            LabelZ = InternalCoordinates ? "Z" : "Up";
        }

        public void RefreshProjects()
        {
            var projectsItems = new ObservableCollection<ProjectItem>();
            var names = new ObservableCollection<string>();
            string[] files = Directory.GetFiles(App.DataFolder);
            foreach (string file in files)
            {
                //Debug.WriteLine($"[Debug]: {file}");
                var fullName = Path.GetFileName(file);
                var projectName = Path.GetFileNameWithoutExtension(file);
                var creationTime = File.GetCreationTime(file);
                var isProject = fullName.Contains(Project.DefaultExtension);
                var project = new ProjectItem { Name = projectName, Date = creationTime, ItemPath = file, Type = isProject ? ItemType.File : ItemType.Folder };
                projectsItems.Add(project);
                names.Add(projectName);
            }
            Projects = projectsItems;
            ExistingSets = names;

            NewSetName = $"Set {Projects.Count + 1}";
            if (UseNew)
            {
                NewChartName = "Chart 1";
            }
            else if (ExistingSetIndex >= 0 && ExistingSetIndex < Projects.Count)
            {
                selectedSet = Projects[ExistingSetIndex];
                var project = Project.Read(selectedSet?.ItemPath);
                NewChartName = $"Chart {(project?.DataCharts?.Count ?? 0) + 1}";
            }
        }

        #endregion

        #region Chart

        public void UpdateChart()
        {
            //Debug.WriteLine($"[Debug]: Update chart: count={Data.Count}");
            var series = new List<ISeries>();
            if (Data?.Count > 0)
            {
                var values = UpdateFFT(); 
                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = values,
                    Stroke = new SolidColorPaint(Utilities.ConvertToSKColor("Red")) { StrokeThickness = 2 },
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null,
                    GeometrySize = 0,
                    LineSmoothness = 0
                });
                HasData = true;
                if (IsImperial)
                    YAxis.Name = DataType == DataType.Displacement ? "Displacement(mil pk-pk)" : 
                                DataType == DataType.Velocity ? "Velocity(in/s rms)" : "Acceleration(G pk)";
                else
                    YAxis.Name = DataType == DataType.Displacement ? "Displacement(μm pk-pk)" :
                                DataType == DataType.Velocity ? "Velocity(mm/s rms)" : "Acceleration(G pk)";
            }
            Lines = series.ToArray();
        }

        List<ObservablePoint> UpdateFFT()
        {
            HasData = false;
            var values = new List<ObservablePoint>();
            if (Data == null || Data.Count == 0) return values;

            var scale = DataType == DataType.Displacement ? 9.8 * 2e6 / (IsImperial ? 25.4 : 1) :
                        DataType == DataType.Velocity ? 9.8 * 1e3 / (Math.Sqrt(2) * (IsImperial ? 25.4 : 1)) : 1;

            if (Direction == Direction.Magnitude)
            {
                var waveformX = Data.Select(p => GetValue(p, Direction.X)).ToArray(); 
                var spectrumX = FftAnalysis.Emgu(waveformX, WindowType.Hann);
                var waveformY = Data.Select(p => GetValue(p, Direction.Y)).ToArray();
                var spectrumY = FftAnalysis.Emgu(waveformY, WindowType.Hann);
                var waveformZ = Data.Select(p => GetValue(p, Direction.Z)).ToArray();
                var spectrumZ = FftAnalysis.Emgu(waveformZ, WindowType.Hann);

                var span = spectrumX.GetLength(1);
                for (int i = 5; i < span / 2; ++i)
                {
                    var dispX = FftAnalysis.Complex(spectrumX, 0, 2 * i - 1);
                    var dispY = FftAnalysis.Complex(spectrumY, 0, 2 * i - 1);
                    var dispZ = FftAnalysis.Complex(spectrumZ, 0, 2 * i - 1);
                    var disp = Math.Sqrt(Math.Pow(dispX.Magnitude, 2) + Math.Pow(dispY.Magnitude, 2) + Math.Pow(dispZ.Magnitude, 2));
                    var freq = i * SampleRate / span;
                    if (DataType == DataType.Velocity)
                    {
                        disp /= 2 * Math.PI * freq;
                    }
                    else if (DataType == DataType.Displacement)
                    {
                        disp /= Math.Pow(2 * Math.PI * freq, 2);
                    }

                    if (disp > 0)
                    {
                        if (ChartType == ChartType.Linear)
                            values.Add(new ObservablePoint(freq, disp * scale));
                        else
                            values.Add(new ObservablePoint(freq, Math.Log10(disp * scale)));
                    }
                }
            }
            else
            {
                var waveform = Data.Select(p => GetValue(p, Direction)).ToArray();
                var spectrum = FftAnalysis.Emgu(waveform, WindowType.Hann);

                var span = spectrum.GetLength(1);
                for (int i = 5; i < span / 2; ++i)
                {
                    var disp = FftAnalysis.Complex(spectrum, 0, 2 * i - 1).Magnitude;
                    var freq = i * SampleRate / span;
                    if (DataType == DataType.Velocity)
                    {
                        disp /= 2 * Math.PI * freq;
                    }
                    else if (DataType == DataType.Displacement)
                    {
                        disp /= Math.Pow(2 * Math.PI * freq, 2);
                    }
                    if (disp > 0)
                    {
                        if (ChartType == ChartType.Linear)
                            values.Add(new ObservablePoint(freq, disp * scale));
                        else
                            values.Add(new ObservablePoint(freq, Math.Log10(disp * scale)));
                    }
                }
            }

            return values;
        }

        double GetValue(Vector3 vector, Direction direction = Direction.Magnitude)
        {
            switch (direction)
            {
                case Direction.X: return vector.X;
                case Direction.Y: return vector.Y;
                case Direction.Z: return vector.Z;
                default: return vector.Length();
            }
        }

        #endregion

        #region Data

        public void Start()
        {
            IsRecording = true;
            YAxis.MaxLimit = null;
            Data.Clear();
            orientationService?.Stop();
            accelerometerService?.Start();
            start = default;
            Count = 0;
            refreshCount = 0;
            Quaternion = orientationService?.Quaternion ?? new Quaternion();
            //Debug.WriteLine($"[Debug]: Start recording");
        }

        public void Stop()
        {
            IsRecording = false;
            accelerometerService?.Stop();
            orientationService?.Start();
            //Debug.WriteLine($"[Debug]: Stop recording, count={Count}");
        }

        public void StartOrientationService()
        {
            orientationService?.Start();
        }

        public void StopOrientationService()
        {
            orientationService?.Stop();
        }

        void AccelerometerChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var acceleration = e.Reading.Acceleration;
            //Debug.WriteLine($"X: {acceleration.X}, Y: {acceleration.Y}, Z: {acceleration.Z}");
            UpdateData(acceleration);
            Update();
        }

        void OrientationChanged(object sender, EventArgs args)
        {
            if (sender is OrientationService service)
            {
                Pitch = Math.Round(service.Roll);
                Roll = Math.Round(service.Pitch);
                Yaw = Math.Round(service.Yaw);
            }
        }

        void UpdateData(Vector3 value)
        {
            Data.Add(value);
            ++Count;
            var maxCount = MaxDuration * SampleRate;
            while (Data.Count > maxCount) Data.RemoveAt(0);
        }

        void Update()
        {
            var stop = DateTime.Now;
            if (start == default) start = stop;
            var secs = (stop - start).TotalSeconds;
            if (secs > 0 && Count > 1) SampleRate = (Count - 1) / secs;
            if (secs > (refreshCount + 1) * MinDuration)
            {
                ++refreshCount;
                UpdateChart();
            }
        }

        #endregion

        #region Auto-wired

        [RelayCommand]
        void Help()
        {
            ShowChart = false;
            ShowSaving = false;
            ShowHelp = true;
            ShowSettings = false;
        }

        [RelayCommand]
        void Zoom()
        {
            XAxis.MinLimit = null;
            XAxis.MaxLimit = null;
            YAxis.MinLimit = null;
            YAxis.MaxLimit= null;
            UpdateChart();
        }

        [RelayCommand]
        void Clear()
        {
            Data.Clear();
            UpdateChart();
        }

        [RelayCommand]
        void Save()
        {
            RefreshProjects();
            ShowChart = false;
            ShowSaving = true;
            ShowHelp = false;
            ShowSettings = false;
        }

        [RelayCommand]
        void StartStop()
        {
            if (IsRecording) Stop();
            else Start();
        }

        [RelayCommand]
        void Settings()
        {
            ShowChart = false;
            ShowSaving = false;
            ShowHelp = false;
            ShowSettings = true;
        }

        [RelayCommand]
        void SaveChart()
        {
            try
            {
                if (UseNew)
                {
                    if (Project.Create(NewSetName, out var project, out var duplicate))
                    {
                        var chart = ChartData.Create(NewChartName, Data, SampleRate, Quaternion);
                        if (chart != null)
                        {
                            chart.Quaternion = Quaternion;
                            project.DataCharts.Add(chart);
                            project.Save();
                        }
                    }
                }
                else if (Projects.Count > ExistingSetIndex && ExistingSetIndex >= 0)
                {
                    var projectItem = Projects[ExistingSetIndex];
                    if (projectItem != null)
                    {
                        var project = Project.Read(projectItem.ItemPath);
                        if (project != null)
                        {
                            var chart = ChartData.Create(NewChartName, Data, SampleRate, Quaternion);
                            if (chart != null)
                            {
                                chart.Quaternion = Quaternion;
                                project.DataCharts.Add(chart);
                                project.Save();
                            }
                        }
                    }
                }
            }
            finally
            {
                ShowChart = true;
                ShowSaving = false;
                ShowHelp = false;
                ShowSettings = false;
            }
        }

        [RelayCommand]
        void GoBack()
        {
            ShowChart = true;
            ShowSaving = false;
            ShowHelp = false;
            ShowSettings = false;
        }

        #endregion
    }
}
