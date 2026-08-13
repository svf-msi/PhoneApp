using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Models
{
    public class Calibration
    {
        public int MediaWidth { get; set; }

        public int MediaHeight { get; set; }

        [JsonIgnore]
        public double AspectRatio => MediaHeight > 0 ? (double)MediaWidth / MediaHeight : 0;

        double width;
        public double FovWidth { get => width; set { if (value != FovWidth) { width = value; height = AspectRatio > 0 ? Math.Round(FovWidth / AspectRatio, 1) : 0; } } }

        double height;
        public double FovHeight { get => height; set { if (value != FovHeight) { height = value; width = Math.Round(FovHeight * AspectRatio, 1); } } }

        [JsonIgnore]
        public bool WidthCalibrated => FovWidth > 0 && MediaWidth > 0;

        [JsonIgnore]
        public bool HeightCalibrated => FovHeight > 0 && MediaHeight > 0;

        [JsonIgnore]
        public bool Calibrated => WidthCalibrated || HeightCalibrated;

        [JsonIgnore]
        public double Scale => WidthCalibrated ? FovWidth / MediaWidth : HeightCalibrated ? FovHeight / MediaHeight : 1;

        public DistanceUnits InputUnits { get; set; } = DistanceUnits.inches;
    }

    public enum DistanceUnits { inches, feet, cm, meters }
}
