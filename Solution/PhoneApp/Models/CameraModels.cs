using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Models
{
    #region Enums

    public enum CameraFacing
    {
        Back,
        Front
    }

    public enum CameraState
    {
        Closed,
        Opening,
        Ready,
        Previewing,
        Recording,
        Error
    }

    #endregion

    #region Value types

    public struct RangeInfo
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Step { get; set; }
        public double Default { get; set; }
        public bool Supported { get; set; }

        public RangeInfo(double min, double max, double step = 0, double def = 0, bool supported = true)
        {
            Min = min; Max = max; Step = step; Default = def; Supported = supported;
        }

        public double Clamp(double value) => value < Min ? Min : value > Max ? Max : value;

        public static RangeInfo Unsupported => new RangeInfo(0, 0, 0, 0, false);

        public override string ToString()
        {
            return $"min={Min}, max={Max}, step={Step}, default={Default}, supported={Supported}";
        }
    }

    public struct Resolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int MaxFrameRate { get; set; }

        public Resolution(int width, int height, int maxFrameRate = 30)
        {
            Width = width; Height = height; MaxFrameRate = maxFrameRate;
        }

        public override string ToString() => $"{Width}x{Height} @ {MaxFrameRate}fps";
    }

    #endregion

    #region Capabilities

    public class CameraCapabilities
    {
        public RangeInfo ExposureRange { get; set; } = RangeInfo.Unsupported;
        public RangeInfo GainRange { get; set; } = RangeInfo.Unsupported;
        public RangeInfo FrameRateRange { get; set; } = RangeInfo.Unsupported;

        public bool SupportsManualExposure => ExposureRange.Supported;
        public bool SupportsManualGain => GainRange.Supported;

        public List<double> FrameRates { get; set; } = new List<double>();
        public List<Resolution> HighSpeedModes { get; set; } = new List<Resolution>();
        public List<double> HighSpeedFrameRates => HighSpeedModes.Select(l => (double)l.MaxFrameRate).Distinct().ToList();
        public List<double> AllFrameRates => FrameRates.Concat(HighSpeedFrameRates).ToList();

        public List<Resolution> Resolutions { get; set; } = new List<Resolution>();

        public override string ToString()
        {
            return $"Exposure range: {ExposureRange}, manual: {SupportsManualExposure} \nGain range: {GainRange}, manual: {SupportsManualGain}\nFrame rate range: {FrameRateRange}\n"
                +  $"Slow frame rates: {string.Join(",", FrameRates)}\nHigh frame modes: {string.Join(",", HighSpeedModes)}\nResolutions: {string.Join(",", Resolutions)}";
        }
    }

    #endregion

    #region Recording

    public class RecordingInfo
    {
        public string Path { get; set; } = "";
        public double FrameRate { get; set; } = -1;
        public double Exposure { get; set; } = -1;
        public double Gain { get; set; } = -1;
    }

    #endregion

    #region Settings

    public class CameraSettings
    {
        public CameraFacing Facing { get; set; } = CameraFacing.Back;
        public double FrameRate { get; set; } = 30;
        public double Exposure { get; set; } = 8000;
        public double Gain { get; set; } = 100;
        public bool AutoExposure { get; set; } = true;
        public bool SlowMotionEnabled { get; set; } = false;
        public Resolution Resolution { get; set; } = new Resolution(1920, 1080, 30);

        public static CameraSettings Default() => new CameraSettings();

        public CameraSettings Clone() => (CameraSettings)MemberwiseClone();
    }

    #endregion

}
