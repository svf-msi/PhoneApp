using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardLib
{
    public class BackgroundAnalysis
    {
        protected static VectorBuilder<double> V = Vector<double>.Build;

        [JsonIgnore]
        public bool Empty => Path == null || Path.Count == 0;
        [JsonIgnore]
        public int Length => Path?.Count ?? 0;
        Dictionary<int, TrackPoint> path;
        [JsonIgnore]
        public Dictionary<int, TrackPoint> Path
        {
            get => path;
            set
            {
                path = value;
            }
        }
        [JsonIgnore]
        public Dictionary<int, TransformPoint> TransformPoins
        {
            get
            {
                if (Path == null) return null;
                var transforms = new Dictionary<int, TransformPoint>();
                foreach (var i in Path.Keys)
                {
                    var transform = new Transform(Path[i]);
                    var invT = transform.InvT;
                    transforms[i] = new TransformPoint { X = invT.At(0), Y = invT.At(1), Angle = -transform.Angle };
                }
                return transforms;
            }
        }

        public void Process(IEnumerable<Target> targets)
        {
            // Process background regions and make a background waveform
            var averagedPath = new Dictionary<int, TrackPoint>();
            var backgrounds = targets?.Where(t => t?.IsBackground == true && t.Track?.RawPath?.Count > 1)?.ToList();
            var count = backgrounds?.Count ?? 0;
            if (count > 0)
            {
                var minFrame = int.MaxValue;
                var maxFrame = int.MinValue;
                var offsets = new Dictionary<Target, TrackPoint>();

                foreach (var target in targets)
                {
                    minFrame = Math.Min(minFrame, target.Track.RawPath.Keys.Min());
                    maxFrame = Math.Max(maxFrame, target.Track.RawPath.Keys.Max());
                }
                var currentPoint = new TrackPoint { Frame = minFrame };
                
                for (int i = minFrame; i <= maxFrame; ++i)
                {
                    var points = new List<TrackPoint>();
                    foreach (var target in backgrounds)
                    {
                        if (target.Track.RawPath.ContainsKey(i) && offsets.ContainsKey(target))
                        {
                            points.Add(target.GetShift(i, offsets[target]));
                        }
                    }

                    if (points.Count > 0)
                    {
                        currentPoint = averagedPath[i] = new TrackPoint
                        {
                            Frame = i,
                            X = points.Select(p => p.X).Average(),
                            Y = points.Select(p => p.Y).Average(),
                        };
                    }
                    else
                    {
                        averagedPath[i] = currentPoint;
                    }

                    if (offsets.Count < count)
                    {
                        foreach (var target in backgrounds)
                        {
                            if (!offsets.ContainsKey(target) && target.Track.RawPath.ContainsKey(i))
                            {
                                offsets[target] = currentPoint;
                            }
                        }
                    }

                    //Console.WriteLine($"{currentPoint.X}, {currentPoint.Y}");
                }
            }
            Path = averagedPath;
        }

        public void Process2D(IEnumerable<Target> targets) // TODO: modify
        {
            // Process background regions and make a background correction
            var backgrounds = targets?.Where(target => target != null && target.IsBackground && target.Track.RawPath?.Count > 0 && target.IsValid)?.ToList();
            var count = backgrounds?.Count ?? 0;

            //Console.WriteLine($"Process background: 2D = {count > 1}");
            if (count == 0) // 1D case
            {
                Path = new Dictionary<int, TrackPoint>();
            }
            else if (count == 1) // 1D case
            {
                Process(targets);
            }
            else if (count > 1) // 2D case
            {
                var transforms = new Dictionary<int, Transform>();
                var minFrame = int.MaxValue;
                var maxFrame = int.MinValue;
                var origins = new Dictionary<Target, TrackPoint>();

                foreach (var target in targets)
                {
                    minFrame = Math.Min(minFrame, target.Track.RawPath.Keys.Min());
                    maxFrame = Math.Max(maxFrame, target.Track.RawPath.Keys.Max());
                }

                var currentTransform = new Transform();

                for (int i = minFrame; i <= maxFrame; ++i)
                {
                    var points = new List<TrackPoint>(); 
                    var priors = new List<TrackPoint>();
                    Target currentTarget = null;

                    foreach (var target in backgrounds)
                    {
                        if (target.Track.RawPath.ContainsKey(i) && origins.ContainsKey(target))
                        {
                            currentTarget = target;
                            points.Add(target.GetRawCGPoint(i));
                            priors.Add(origins[target]);
                        }
                    }

                    if (points.Count > 1) // multiple points - 2D
                    {
                        var stabilization = new Stabilization(points, priors);
                        currentTransform = stabilization.IsValid ? stabilization.Transform : currentTransform.Copy();
                    }
                    else if (points.Count > 0 && currentTarget != null) // single point - 1D
                    {
                        var priorFrame = currentTarget.GetPriorFrame(i);
                        if (priorFrame > -1 && transforms.ContainsKey(priorFrame))
                        {
                            var transform = transforms[priorFrame].Copy();
                            var currentPosition = currentTarget.GetRawCGPoint(i);
                            var prirorPosition = currentTarget.GetRawCGPoint(priorFrame);
                            transform.X += currentPosition.X - prirorPosition.X;
                            transform.Y += currentPosition.Y - prirorPosition.Y;
                            currentTransform = transform;
                        }
                        else
                            currentTransform = currentTransform.Copy();
                    }
                    else // no points - 0D (constant)
                    {
                        currentTransform = currentTransform.Copy();
                    }
                    
                    //Console.WriteLine($"{currentTransform.X}, {currentTransform.Y}, {currentTransform.Angle}");
                    transforms[i] = currentTransform;

                    if (origins.Count < count)
                    {
                        foreach (var target in backgrounds)
                        {
                            if (!origins.ContainsKey(target) && target.Track.RawPath.ContainsKey(i))
                            {
                                var point = target.GetRawCGPoint(i);
                                origins[target] = currentTransform.Backward(point);
                            }
                        }
                    }
                }

                var backgroundPath = new Dictionary<int, TrackPoint>();
                for (int i = 0; i < transforms.Count; ++i)
                {
                    backgroundPath[i + minFrame] = new TrackPoint()
                    {
                        Frame = i + minFrame,
                        X = (float)transforms[i].X,
                        Y = (float)transforms[i].Y,
                        Angle = (float)transforms[i].Angle,
                    };
                }
                Path = backgroundPath;
            }
        }

        public void Subtract(IEnumerable<Target> targets)
        {
            if (targets == null) return;

            foreach (var target in targets)
            {
                var region = target;
                if (Length > 0 && target != null && !target.IsBackground && target.Track.RawPath?.Count > 0)
                {
                    var analysisWaveform = new Dictionary<int, TrackPoint>();
                    var keys = target.Track.RawPath.Keys.OrderBy(k => k).ToList();

                    var start = new TrackPoint();
                    if (Path.ContainsKey(keys[0]))
                    {
                        start = new TrackPoint
                        {
                            Frame = keys[0],
                            X = Path[keys[0]].X,
                            Y = Path[keys[0]].Y,
                            Angle = Path[keys[0]].Angle,
                        };
                    }

                    foreach (var i in keys)
                    {
                        if (Path.ContainsKey(i))
                        {
                            var transform = new Transform(Path[i]);
                            var point = target.GetRawCGPoint(i);
                            point = transform.Backward(point);
                            var offset = target.GradientPointsCGOffset;
                            point.X -= offset.X;
                            point.Y -= offset.Y;
                            analysisWaveform[i] = point;
                        }
                        else
                        {
                            analysisWaveform[i] = target.Track.RawPath[i].Copy();
                        }
                    }
                    target.Track.Path = analysisWaveform;
                }
                else
                {
                    //Console.WriteLine($"Check no background: {Utils.ToString(target.Track.RawPath)}");
                    if (target?.Track.RawPath != null && !target.IsBackground)
                        target.Track.Path = target.Track.RawPath.ToDictionary(p => p.Key, p => p.Value.Copy());
                    //Console.WriteLine($"Check no background: {Utils.ToString(target.Track.Path)}");
                }
            }
        }
    }
}
