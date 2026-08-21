using CommunityToolkit.Mvvm.ComponentModel;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Models
{
    public partial class Region : ObservableObject
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        string name = "Region";

        [ObservableProperty]
        Rect rect;

        [ObservableProperty]
        double size = 50;

        [ObservableProperty]
        double x;

        [ObservableProperty]
        double y;

        [ObservableProperty]
        bool isBackgound;

        [ObservableProperty]
        Color color = Colors.Red;

        [ObservableProperty]
        string colorText = Colors.Red.ToString();

        public Region(int id, string name, double size, double x, double y, bool isBackgound, string colorText)
        {
            Id = id;
            Name = name;
            Size = size;
            X = x;
            Y = y;
            IsBackgound = isBackgound;
            ColorText = colorText;
        }

        public Target ToTarget(int frame = 0)
        {
            var target = new Target
            {
                Name = Name,
                ColorText = ColorText,
                IsBackground = IsBackgound,
                Reference = ToTrackRegion(frame)
            };
            target.Track.TargetName = target.Name;
            return target;
        }

        public TrackRegion ToTrackRegion(int frame = 0)
        {
            return new TrackRegion
            {
                Name = Name,
                FrameNumber = frame,
                X = (float)X,
                Y = (float)Y,
                Width = (float)Size,
                Height = (float)Size
            };
        }

        void SetRect()
        {
            Rect = new Rect(X - Size / 2, Y - Size / 2, Size, Size);
        }

        partial void OnColorTextChanged(string? oldValue, string newValue)
        {
            if (newValue != oldValue)
            {
                if (Color.TryParse(newValue, out Color color))
                {
                    Color = color;
                }
            }
        }

        partial void OnSizeChanged(double oldValue, double newValue)
        {
            SetRect();
        }

        partial void OnXChanged(double oldValue, double newValue)
        {
            SetRect();
        }

        partial void OnYChanged(double oldValue, double newValue)
        {
            SetRect();
        }
    }
}
