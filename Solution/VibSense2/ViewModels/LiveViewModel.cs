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
        [ObservableProperty]
        int count = 0;

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
        bool hasData = false;

        int refreshCount;
        DateTime start;
        double minDuration = 1;
        double maxDuration = 10;

        public double SampleRate { get; set; } = 400;

        public List<Vector3> Data { get; set; } = new List<Vector3>();

        AccelerometerService accelerometerService;


        #region Chart-related

        public Axis[] XAxes { get; set; }

        public Axis XAxis { get; set; }

        public Axis[] YAxes { get; set; }

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
        }

        [ObservableProperty]
        DataType dataType = DataType.Acceleration;
        partial void OnDataTypeChanged(DataType value)
        {
            Preferences.Set(App.SettingType, (int)DataType);
        }

        #endregion

        public LiveViewModel()
        {
            accelerometerService = new AccelerometerService();
            accelerometerService.AccelerometerChanged += AccelerometerChanged;

            InitializeChart();
            InitializeSettings();
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

            YAxis = new Axis
            {
                Name = DataType == DataType.Displacement ? "Displacement" : DataType == DataType.Velocity ? "Velocity" : "Acceleration",
                TextSize = 18,
                NamePadding = new LiveChartsCore.Drawing.Padding(0, 5, 0, -10),
                NamePaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                LabelsPaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
                SubseparatorsPaint = new SolidColorPaint(new SKColor(160, 160, 160)) { StrokeThickness = 0.5f },
                TicksPaint = new SolidColorPaint(new SKColor(90, 90, 90)) { StrokeThickness = 1 },
            };
            YAxes = new Axis[] { YAxis };
            Lines = new ISeries[] { };
        }

        void InitializeSettings()
        {
            ChangeDirection((Direction)Preferences.Get(App.SettingDirection, 0));
            ChangeType((DataType)Preferences.Get(App.SettingType, 0));
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
        }

        double GetValue(Vector3 vector)
        {
            switch (Direction)
            {
                case Direction.X: return vector.X;
                case Direction.Y: return vector.Y;
                case Direction.Z: return vector.Z;
                default: return vector.Length();
            }
        }

        public void UpdateChart()
        {
            Debug.WriteLine($"[Debug]: Update chart: count={Data.Count}");
            var series = new List<ISeries>();
            var rate = SampleRate;
            if (Data?.Count > 0)
            {
                var waveform = Data.Select(p => GetValue(p)).ToArray();
                var spectrum = FftAnalysis.Emgu(waveform, WindowType.Hann);
                var span = spectrum.GetLength(1);
                var values = new List<ObservablePoint>();
                for (int i = 5; i < span / 2; ++i)
                {
                    var disp = FftAnalysis.Complex(spectrum, 0, 2 * i - 1);
                    values.Add(new ObservablePoint(i * rate / span, disp.Magnitude));
                }
                var s = new LineSeries<ObservablePoint>
                {
                    Values = values,
                    Stroke = new SolidColorPaint(Utilities.ConvertToSKColor("Red")) { StrokeThickness = 2 },
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null,
                    GeometrySize = 0,
                    LineSmoothness = 0
                };
                series.Add(s);
                HasData = true;
                YAxis.Name = DataType == DataType.Displacement ? "Displacement" : DataType == DataType.Velocity ? "Velocity" : "Acceleration";
            }
            Lines = series.ToArray();
        }

        public void Start()
        {
            YAxis.MaxLimit = null;
            Data.Clear();
            accelerometerService.Start();
            start = default;
            Count = 0;
            refreshCount = 0;
            Debug.WriteLine($"[Debug]: Start recording");
        }

        public void Stop()
        {
            accelerometerService.Stop();
            Debug.WriteLine($"[Debug]: Stop recording, count={Count}");
        }

        void AccelerometerChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var acceleration = e.Reading.Acceleration;
            //Debug.WriteLine($"X: {acceleration.X}, Y: {acceleration.Y}, Z: {acceleration.Z}");
            UpdateData(acceleration);
            Update();
        }

        void UpdateData(Vector3 value)
        {
            Data.Add(value);
            ++Count;
            var maxCount = maxDuration * SampleRate;
            while (Data.Count > maxCount) Data.RemoveAt(0);
        }

        void Update()
        {
            var stop = DateTime.Now;
            if (start == default) start = stop;
            var secs = (stop - start).TotalSeconds;
            if (secs > 0 && Count > 1) SampleRate = (Count - 1) / secs;
            if (secs > (refreshCount + 1) * minDuration)
            {
                //if (refreshCount == 1)
                //{
                //    if (YAxis.MaxLimit == null)
                //    {
                //        var ylimits = YAxis.GetLimits();
                //        YAxis.MaxLimit = ylimits.Max;
                //    }
                //}
                ++refreshCount;
                UpdateChart();
            }
        }

        [RelayCommand]
        void Help()
        {

        }

        [RelayCommand]
        void Clear()
        {

        }

        [RelayCommand]
        void Save()
        {

        }
    }

    public enum Direction { Magnitude, X, Y, Z }

    public enum DataType { Displacement, Velocity, Acceleration }
}
