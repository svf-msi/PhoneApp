using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StandardLib
{
    public class Track 
    {
        #region Fields and Properties

        public string TargetName { get; set; }

        #region Path-related

        int currentFrame;
        [JsonIgnore]
        public int CurrentFrame 
        { 
            get => currentFrame; 
            set 
            { 
                currentFrame = value;
            }
        }

        [JsonIgnore]
        public TrackPoint CurrentPoint
        {
            get 
            {
                if (RawPath?.ContainsKey(CurrentFrame) == true) 
                    return RawPath[CurrentFrame]; 
                else return null; 
            }
            set 
            { 
                if (RawPath == null) RawPath = new Dictionary<int, TrackPoint>(); 
                RawPath[CurrentFrame] = value;
            }
        }

        [JsonIgnore]
        public PointState CurrentState { get => CurrentPoint?.State ?? PointState.None; set { if (CurrentPoint != null) CurrentPoint.State = value; } }

        Dictionary<int, TrackPoint> rawPath = new Dictionary<int, TrackPoint>();
        public Dictionary<int, TrackPoint> RawPath
        {
            get => rawPath;
            set
            {
                rawPath = value;
            }
        }

        public List<TrackPoint> RawPoints => RawPath?.Values.OrderBy(p => p.Frame).ToList() ?? new List<TrackPoint>();

        Dictionary<int, TrackPoint> path;
        [JsonIgnore]
        public Dictionary<int, TrackPoint> Path { get => path ?? RawPath; set => path = value; }

        #endregion

        #region Analysis-related

        public double Exposure { get; set; } = 0; // secs
        public double TimeInterval { get; set; } = 1; // secs
        [JsonIgnore]
        public List<FDpoint> Spectrum { get; set; }
        [JsonIgnore]
        public WindowType WindowType { get; set; } = WindowType.Hann;

        object pathLock = new object();
        object spectrumLock = new object();

        #endregion

        #endregion

        public Track() {}

        public void Setup(double interval = 1, double exposure = 0)
        {
            RawPath = new Dictionary<int, TrackPoint>();
            TimeInterval = interval;
            Exposure = exposure;
        }

        public TrackPoint GetLocalShift(int frame)
        {
            var point = new TrackPoint();
            if (RawPath != null && RawPath.ContainsKey(frame) && RawPath.ContainsKey(frame - 1))
            {
                var current = RawPath[frame];
                var prior = RawPath[frame - 1];
                point.Frame = frame;
                point.Time = current.Time;
                point.X = current.X - prior.X;
                point.Y = current.Y - prior.Y;
            }
            return point;
        }

        public void Fft(WindowType windowType = WindowType.Hann)
        {
            if (Path == null || Path.Count < 4) return;

            lock (spectrumLock)
            {
                var start = Path.Keys.Min();
                var end = Path.Keys.Max();
                var waveform = new List<TrackPoint>();
                var startPoint = Path[start];
                var currentPoint = startPoint.Offset(startPoint.X, startPoint.Y); ;
                for (int i = start; i <= end; ++i)
                {
                    if (Path.ContainsKey(i))
                    {
                        currentPoint = Path[i].Offset(startPoint.X, startPoint.Y);
                    }
                    waveform.Add(currentPoint);
                }

                var spectrum = new List<FDpoint>();
                WindowType = windowType;

                try
                {
                    var padFour = true; // add extra points to make length divisible by 4
                    var waveformX = waveform.Select(v => (double)v.X).ToArray();
                    var spectrumX = FftAnalysis.Emgu(waveform.Select(v => (double)v.X).ToArray(), windowType, padFour);
                    var spectrumY = FftAnalysis.Emgu(waveform.Select(v => (double)v.Y).ToArray(), windowType, padFour);
                    var timeInterval = TimeInterval;
                    var span = spectrumX.Length;

                    // Exposure compensation
                    var exposureFactor = TimeInterval <= 0 ? 0 : Exposure / TimeInterval;
                    var compensation = Math.PI * Math.Min(exposureFactor, 1) / span;

                    spectrum.Add(new FDpoint
                    {
                        Frequency = 0,
                        X = Math.Abs(spectrumX[0, 0]),
                        Y = Math.Abs(spectrumY[0, 0]),
                        Magnitude = Math.Sqrt(spectrumX[0, 0] * spectrumX[0, 0] + spectrumY[0, 0] * spectrumY[0, 0]),
                        PhaseX = 0,
                        PhaseY = 0
                    });

                    // Populate spectrum data fields
                    for (int t = 1; t < span / 2; ++t)
                    {
                        var comp = compensation > 0 ? Math.Sin(compensation * t) / (compensation * t) : 1;
                        var x = FftAnalysis.Complex(spectrumX, 0, 2 * t - 1);
                        var y = FftAnalysis.Complex(spectrumY, 0, 2 * t - 1);
                        spectrum.Add(new FDpoint
                        {
                            Frequency = t / (timeInterval * span),
                            X = x.Magnitude / comp,
                            Y = y.Magnitude / comp,
                            Magnitude = Math.Sqrt(x.Magnitude * x.Magnitude + y.Magnitude * y.Magnitude) / comp,
                            PhaseX = x.Phase,
                            PhaseY = y.Phase
                        });
                    }

                    spectrum.Add(new FDpoint
                    {
                        Frequency = 1 / timeInterval / 2,
                        X = Math.Abs(spectrumX[0, span - 1]),
                        Y = Math.Abs(spectrumY[0, span - 1]),
                        Magnitude = Math.Sqrt(spectrumX[0, span - 1] * spectrumX[0, span - 1] + spectrumY[0, span - 1] * spectrumY[0, span - 1]),
                        PhaseX = 0,
                        PhaseY = 0
                    });

                    Spectrum = spectrum;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in frequency analysis: {e}");
                }
            }
        }

        public void ClearFrom(int frame)
        {
            var keys = RawPath.Keys.ToArray();
            foreach (var key in keys)
            {
                if (key >= frame)
                    RawPath.Remove(key);
            }
        }
    }

    public class TDpoint
    {
        public double Time { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }
        public PointState PointState { get; set; }
        [JsonIgnore]
        public double Magnitude { get => Math.Sqrt(X * X + Y * Y); }

        public TDpoint Copy() => MemberwiseClone() as TDpoint;
    }

    public class FDpoint
    {
        public double Frequency { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double PhaseX { get; set; }
        public double PhaseY { get; set; }
        public double Magnitude { get; set; }
        public double AngleX => PhaseX * 180 / Math.PI;
        public double AngleY => PhaseY * 180 / Math.PI;
    }
}
