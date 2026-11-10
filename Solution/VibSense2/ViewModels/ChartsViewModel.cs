using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
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
    [QueryProperty(nameof(ProjectItem), "ProjectItem")]
    public partial class ChartsViewModel : ObservableObject
    {
        #region Fields and Properties

        #region Common

        List<string> colors = new List<string>() { "Red", "Green", "Blue", "Magenta", "Olive", "Purple" };

        [ObservableProperty]
        string title = "Charts";

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
        ObservableCollection<ChartData> charts = new ObservableCollection<ChartData>();

        [ObservableProperty]
        ProjectItem projectItem = new ProjectItem();
        partial void OnProjectItemChanged(ProjectItem projectItem)
        {
            Initialize(projectItem);
        }

        Project project;

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

        #endregion

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
            Debug.WriteLine($"[Debug]: direction changed to {Direction}");
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

        #endregion

        #endregion

        #region Setup

        public ChartsViewModel()
        {
            ReadHelp();
            InitializeChart();
            InitializeSettings();
        }

        void Initialize(ProjectItem item)
        {
            //Debug.WriteLine($"[Debug]: Initialize projectitem {item?.Name}");

            if (project?.DataCharts != null)
            {
                foreach (var chart in project.DataCharts)
                {
                    chart.PropertyChanged -= Chart_PropertyChanged;
                }
            }

            project = Project.Read(item?.ItemPath);

            if (project?.DataCharts != null)
            {
                Charts = new ObservableCollection<ChartData>(project.DataCharts);
                foreach (var chart in Charts)
                {
                    chart.PropertyChanged += Chart_PropertyChanged;
                }
                UpdateChart();
            }

            Title = project?.Name ?? "Charts";
        }

        void Chart_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (sender is ChartData chart && args.PropertyName == nameof(chart.Selected))
            {
                UpdateChart();
            }
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
            var units = Preferences.Get(App.SettingUnits, 0);
            IsMetric = units != 0;
            IsImperial = units == 0;
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
            //UpdateChart();
        }

        void ReadHelp()
        {
            Task.Run(async () =>
            {
                try
                {
                    using Stream stream = await FileSystem.Current.OpenAppPackageFileAsync("help2.txt");
                    using StreamReader reader = new StreamReader(stream);

                    HelpText = await reader.ReadToEndAsync();
                }
                catch (FileNotFoundException)
                {
                    HelpText = "Help file not found.";
                }
            });
        }

        #endregion

        #region Chart

        public void UpdateChart()
        {
            Debug.WriteLine($"[Debug]: Update chart, direction={Direction}, type={DataType}");
            var series = new List<ISeries>();
            if (project?.DataCharts?.Count > 0)
            {
                for (int i = 0; i < project.DataCharts.Count; ++i)
                {
                    var chart = project.DataCharts[i];
                    if (!chart.Selected) continue;

                    var values = UpdateData(chart);
                    var color = colors[i % colors.Count];
                    series.Add(new LineSeries<ObservablePoint>
                    {
                        Name = chart.Name,
                        Values = values,
                        Stroke = new SolidColorPaint(Utilities.ConvertToSKColor(color)) { StrokeThickness = 2 },
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        GeometrySize = 0,
                        LineSmoothness = 0
                    });
                }
            }
            Lines = series.ToArray();

            if (IsImperial)
                YAxis.Name = DataType == DataType.Displacement ? "Displacement(mil pk-pk)" :
                            DataType == DataType.Velocity ? "Velocity(in/s rms)" : "Acceleration(G pk)";
            else
                YAxis.Name = DataType == DataType.Displacement ? "Displacement(μm pk-pk)" :
                            DataType == DataType.Velocity ? "Velocity(mm/s rms)" : "Acceleration(G pk)";
        }

        List<ObservablePoint> UpdateData(ChartData chart)
        {
            var values = new List<ObservablePoint>();
            if (chart?.Data == null || chart.Data.Count == 0) return values;

            var scale = DataType == DataType.Displacement ? 9.8 * 2e6 / (IsImperial ? 25.4 : 1) :
                        DataType == DataType.Velocity ? 9.8 * 1e3 / (Math.Sqrt(2) * (IsImperial ? 25.4 : 1)) : 1;

            //Debug.WriteLine($"[Debug]: check={chart.Data[5]}");
            for (int i = 5; i < chart.Data.Count; ++i)
            {
                var p = chart.Data[i];
                var freq = i * chart.SampleRate / 2 / chart.Data.Count;
                double value = 0;
                if (Direction == Direction.X) value = p.X;
                else if (Direction == Direction.Y) value = p.Y;
                else if (Direction == Direction.Z) value = p.Z;
                else value = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
                if (DataType == DataType.Velocity)
                {
                    value /= 2 * Math.PI * freq;
                }
                else if (DataType == DataType.Displacement)
                {
                    value /= Math.Pow(2 * Math.PI * freq, 2);
                }
                values.Add(new ObservablePoint(freq, value * scale));
            }
            return values;
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
            YAxis.MaxLimit = null;
            UpdateChart();
        }

        [RelayCommand]
        void Export()
        {
            
        }

        [RelayCommand]
        void Delete(ChartData chart)
        {
            if (chart != null)
            {
                chart.PropertyChanged -= Chart_PropertyChanged;
                Charts.Remove(chart);
                project.DataCharts.Remove(chart);
                UpdateChart();
                project.Save();
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
