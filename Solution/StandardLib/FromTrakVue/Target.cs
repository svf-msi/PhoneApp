using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StandardLib
{
    public class Target : INotifyPropertyChanged
    {
        #region Fields and Properties

        #region General

        public string Name { get; set; }

        public string ColorText { get; set; }

        bool isBackground;
        public bool IsBackground { get => isBackground; set { if (IsBackground == value) return; isBackground = value; NotifyPropertyChanged(); } }

        bool isShown = true;
        public bool IsShown { get => isShown; set { isShown = value; NotifyPropertyChanged(); } }

        bool isValid = true;
        public bool IsValid { get => isValid; set { if (value == IsValid) return; isValid = value; NotifyPropertyChanged(); } }

        bool isLocked = true;
        public bool IsLocked { get => isLocked; set { if (IsLocked && !value) Reset(); isLocked = value; NotifyPropertyChanged(); } }

        bool isTracked = true;
        public bool IsTracked { get => isTracked; set { isTracked = value; NotifyPropertyChanged(); } }

        int startFrame = 0;
        public int StartFrame { get => startFrame; set { startFrame = value; NotifyPropertyChanged(); } }

        int endFrame = 0;
        public int EndFrame { get => endFrame; set { endFrame = value; NotifyPropertyChanged(); } }

        int completion = 0;
        public int Completion { get => completion; set { completion = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(IsComplete)); } }

        public bool IsComplete => Completion == 100;

        TargetType type = TargetType.Static;
        public TargetType Type { get => type; set { type = value; NotifyPropertyChanged(); } }

        public double TimeInterval { get; set; } = 1;

        #endregion

        #region Reference-related

        double offsetX; // mm
        public double OffsetX { get => offsetX; set { offsetX = value; NotifyPropertyChanged(); } }

        double offsetY; // mm
        public double OffsetY { get => offsetY; set { offsetY = value; NotifyPropertyChanged(); } }

        int primaryReferenceFrame;
        public int PrimaryReferenceFrame { get => primaryReferenceFrame; set { primaryReferenceFrame = StartFrame = value; NotifyPropertyChanged(); } }

        int referenceRange = 1;
        public int ReferenceRange { get => referenceRange; set { referenceRange = value; NotifyPropertyChanged(); } }

        TrackRegion reference;
        public TrackRegion Reference
        {
            get => reference;
            set
            {
                reference = value;
                NotifyPropertyChanged();
                if (Reference != null)
                {
                    References[Reference.FrameNumber] = Reference;
                }
            }
        }

        public double ReferenceError { get; set; }

        [JsonIgnore]
        public double ReferenceX => Reference?.X ?? 0;

        [JsonIgnore]
        public double ReferenceY => Reference?.Y ?? 0;

        [JsonIgnore]
        public double ReferenceWidth => Reference?.Width ?? 0;

        [JsonIgnore]
        public double ReferenceHeight => Reference?.Height ?? 0;

        [JsonIgnore]
        public TrackRegion RoundReference
        {
            get
            {
                if (Reference == null) return null;
                var left = (float)Math.Round(Reference.X - Reference.Width / 2);
                var top = (float)Math.Round(Reference.Y - Reference.Height / 2);
                var width = (float)Math.Round(Reference.Width);
                var height = (float)Math.Round(Reference.Height);

                return new TrackRegion
                {
                    FrameNumber = Reference.FrameNumber,
                    Time = Reference.Time,
                    X = left + width / 2,
                    Y = top + height / 2,
                    Width = width,
                    Height = height
                };
            }
        }

        [JsonIgnore]
        public System.Drawing.PointF ReferenceOffset
        {
            get
            {
                return new System.Drawing.PointF
                {
                    X = Reference.X - (float)(Math.Round(Reference.X - Reference.Width / 2) + Math.Round(Reference.Width) / 2),
                    Y = Reference.Y - (float)(Math.Round(Reference.Y - Reference.Height / 2) + Math.Round(Reference.Height) / 2)
                };
            }
        }

        Dictionary<int, TrackRegion> references = new Dictionary<int, TrackRegion>();
        public Dictionary<int, TrackRegion> References { get => references; set { references = value; NotifyPropertyChanged(); } }

        [JsonIgnore]
        public TrackRegion CurrentReference
        {
            get
            {
                if (IsInRange)
                {
                    return GetReference(PrimaryReferenceFrame);
                }
                else if (CurrentPoint != null)
                {
                    return GetReference(CurrentPoint.ReferenceFrame);
                }
                else
                    return null;
            }
        }

        [JsonIgnore]
        public Image<Gray, float> GrayReference { get; set; }

        [JsonIgnore]
        public Image<Rgb, float> RgbReference { get; set; }

        List<GradientPoint> gradientPoints;

        [JsonIgnore]
        public List<GradientPoint> GradientPoints { get => gradientPoints; set { gradientPoints = value; SetCG(); } } // referenced to reference rectangle

        [JsonIgnore]
        public System.Drawing.PointF GradientPointsCGOffset { get; set; }

        [JsonIgnore]
        public bool HasGoodGradientPoints => GradientPoints?.Count > 2;

        #endregion

        #region Track-related

        public Track Track { get; set; }

        public Dictionary<int, TransformPoint> TransformPoints { get; set; }

        int currentFrame;
        [JsonIgnore]
        public int CurrentFrame
        {
            get => currentFrame; // Track?.CurrentFrame ?? -1;
            set
            {
                if (Track != null) Track.CurrentFrame = value;
                currentFrame = value;

                Console.WriteLine($"Frame changed in {Name}: {currentFrame}");
                if (Type == TargetType.Dynamic)
                {
                    Reference = CurrentReference;
                }
                NotifyPropertyChanged();
                UpdatePoint();
            }
        }

        [JsonIgnore]
        public bool IsInRange => CurrentFrame >= StartFrame && CurrentFrame <= EndFrame;

        [JsonIgnore]
        public TrackPoint CurrentPoint { get => Track?.CurrentPoint; set { if (Track != null) Track.CurrentPoint = value; } }

        [JsonIgnore]
        public float X { get => CurrentPoint?.X ?? 0; set { if (CurrentPoint != null) CurrentPoint.X = value; NotifyPropertyChanged(); } }

        [JsonIgnore]
        public float Y { get => CurrentPoint?.Y ?? 0; set { if (CurrentPoint != null) CurrentPoint.Y = value; NotifyPropertyChanged(); } }

        [JsonIgnore]
        public TrackRegion CurrentRegion => new TrackRegion { Name = Name, FrameNumber = CurrentFrame, X = X, Y = Y, Width = Width, Height = Height };

        [JsonIgnore]
        public float Width => Reference?.Width ?? 0;

        [JsonIgnore]
        public float Height => Reference?.Height ?? 0;

        [JsonIgnore]
        public bool IsSet => CurrentPoint != null;

        [JsonIgnore]
        public PointState PointState { get => CurrentPoint?.State ?? PointState.None; set { if (IsSet) CurrentPoint.State = value; NotifyPropertyChanged(); } }

        [JsonIgnore]
        public bool OrbitUpdate { get; set; }

        [JsonIgnore]
        public double StartX => Track?.RawPath?.ContainsKey(PrimaryReferenceFrame) == true ? Track.RawPath[PrimaryReferenceFrame].X : 0;

        [JsonIgnore]
        public double StartY => Track?.RawPath?.ContainsKey(PrimaryReferenceFrame) == true ? Track.RawPath[PrimaryReferenceFrame].Y : 0;

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public Target()
        {
            Track = new Track();
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") 
        { 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Reference-related

        public TrackRegion GetReference(int frame)
        {
            if (References?.ContainsKey(frame) == true)
            {
                return References[frame];
            }
            else
                return null;
        }

        public void UpdateReference(int frame)
        {
            if (Type == TargetType.Dynamic)
            {
                var reference = GetReference(frame);
                if (reference == null)
                {
                    var previous = GetPoint(frame - 1);
                    if (previous != null)
                    {
                        reference = GetReference(previous.ReferenceFrame);
                        if (frame - previous.ReferenceFrame > ReferenceRange)
                        {
                            reference = previous.MakeTrackRegion(reference.Width, reference.Height);
                            reference.FrameNumber = frame - 1;
                        }
                    }
                }

                if (Reference != reference)
                {
                    //Console.WriteLine($"Update reference: {Utils.ToString(reference)}");
                    Reference = reference;
                    GrayReference = null;
                    RgbReference = null;
                    GradientPoints = null;
                }
            }
            else if (Reference == null && References?.Count > 0)
            {
                Reference = References[0];
            }
        }

        public TrackRegion MakeReference(int frame)
        {
            var point = GetPoint(frame);
            if (point != null)
            {
                var previous = GetReference(point.ReferenceFrame);
                if (previous != null)
                {
                    var reference = point.MakeTrackRegion(previous.Width, previous.Height);
                    reference.FrameNumber = frame;
                    return reference;
                }
            }
            return null;
        }

        void SetCG()
        {
            if (GradientPoints == null || GradientPoints.Count == 0)
                GradientPointsCGOffset = default;
            else
            {
                var x = GradientPoints.Select(p => (float)p.Point.X).Average(); // with respect to round reference
                var y = GradientPoints.Select(p => (float)p.Point.Y).Average();
                var left = (float)Math.Round(Reference.X - Reference.Width / 2);
                var top = (float)Math.Round(Reference.Y - Reference.Height / 2);
                x = left + x - Reference.X;
                y = top + y - Reference.Y;
                GradientPointsCGOffset = new System.Drawing.PointF(x, y);
                //Console.WriteLine($"Set CG in {Text}: {x}, {y}");
            }
        }

        public TrackPoint GetRawCGPoint(int frame)
        {
            if (!Track.RawPath.ContainsKey(frame)) return null;
            var point = Track.RawPath[frame].Copy();
            var offset = GradientPointsCGOffset;
            point.X += offset.X;
            point.Y += offset.Y;
            return point;
        }

        public void ResetPrimaryReference(int frame)
        {
            if (IsLocked || frame == PrimaryReferenceFrame) return;
            if (References != null)
            {
                var reference = References.ContainsKey(PrimaryReferenceFrame) ? References[PrimaryReferenceFrame] : null;
                References.Clear();
                Track.RawPath.Clear();
                if (reference != null)
                {
                    reference.FrameNumber = frame;
                    Reference = reference;
                    Track.RawPath[frame] = reference.TrackPoint;
                    UpdatePoint();
                }
            }
            EndFrame = frame;
            PrimaryReferenceFrame = frame;
        }

        #endregion

        #region Point-related

        public int GetPriorFrame(int frame)
        {
            for (int i = frame - 1; frame >= StartFrame; --frame)
            {
                if (Track.RawPath?.ContainsKey(i) == true) return i;
            }
            return -1;
        }

        public TrackPoint GetShift(int frame, TrackPoint offset = null)
        {
            TrackPoint result = new TrackPoint
            {
                Frame = frame,
                X = 0,
                Y = 0,
                Angle = 0
            };
            
            var point = GetPoint(frame);
            if (point == null) return result;
            var primary = GetPoint(PrimaryReferenceFrame);
            if (primary == null) return result;

            result.X = point.X - primary.X;
            result.Y = point.Y - primary.Y;
            result.Angle = point.Angle - primary.Angle;

            if (offset != null)
            {
                result.X += offset.X;
                result.Y += offset.Y;
                result.Angle += offset.Angle;
            }
            
            return result;
        }

        public TrackPoint GetPoint(int frame)
        {
            if (Track?.RawPath?.ContainsKey(frame) == true)
                return Track.RawPath[frame];
            else
                return null;
        }

        void UpdatePoint()
        {
            NotifyPropertyChanged(nameof(CurrentPoint));
            NotifyPropertyChanged(nameof(CurrentRegion));
        }

        public void Reset()
        {
            Track?.RawPath?.Clear(); Track?.Path?.Clear();
            var reference = (References?.ContainsKey(PrimaryReferenceFrame) == true) ? References[PrimaryReferenceFrame] : null;
            References?.Clear();
            //Console.WriteLine($"Reset: {point}, {reference}, {PrimaryReferenceFrame}");
            if (reference != null)
            {
                Reference = reference;
                Track.RawPath[PrimaryReferenceFrame] = reference.TrackPoint;
            }
            EndFrame = PrimaryReferenceFrame;
        }

        public void ClearFrom(int frame)
        {
            if (frame <= PrimaryReferenceFrame)
            {
                Reset();
            }
            else
            {
                Track.ClearFrom(frame);
                var keys = References.Keys.ToArray();
                foreach (var key in keys)
                {
                    if (key >= frame && key != PrimaryReferenceFrame)
                        References.Remove(key);
                }
                EndFrame = frame - 1;
            }
        }

        void Update(double value, string field)
        {
            //Console.WriteLine($"Target update for {Type}: {field}={value}, frame={CurrentFrame}, ref={PrimaryReferenceFrame}");
            if (Type == TargetType.Static)
            {
                if (CurrentPoint != null && CurrentFrame != PrimaryReferenceFrame)
                {
                    if (field == nameof(CurrentPoint.X))
                    {
                        CurrentPoint.X = (float)Math.Round(value, 3);
                    }
                    else if (field == nameof(CurrentPoint.Y))
                    {
                        CurrentPoint.Y = (float)Math.Round(value, 3);
                    }
                    CurrentPoint.State = PointState.Manual;
                }
                if (!IsLocked && CurrentFrame == PrimaryReferenceFrame)
                {
                    if (field == nameof(CurrentPoint.X))
                    {
                        CurrentPoint.X = (float)Math.Round(value, 3);
                    }
                    else if (field == nameof(CurrentPoint.Y))
                    {
                        CurrentPoint.Y = (float)Math.Round(value, 3);
                    }
                    if (Reference != null)
                    {
                        Reference[field] = (float)Math.Round(value, 3);
                    }
                    if (References?.ContainsKey(PrimaryReferenceFrame) == true)
                    {
                        References[PrimaryReferenceFrame][field] = (float)Math.Round(value, 3);
                    }
                    NotifyPropertyChanged("Reference");
                }
                UpdatePoint();
            }
            else if (Type == TargetType.Dynamic)
            {
                if (CurrentPoint != null)
                {
                    if (field == nameof(CurrentPoint.X))
                    {
                        CurrentPoint.X = (float)Math.Round(value, 3);
                    }
                    else if (field == nameof(CurrentPoint.Y))
                    {
                        CurrentPoint.Y = (float)Math.Round(value, 3);
                    }
                    if (CurrentFrame != PrimaryReferenceFrame && CurrentFrame != EndFrame)
                    {
                        CurrentPoint.State = PointState.Manual;
                    }
                }
                if (!IsLocked && CurrentFrame == PrimaryReferenceFrame)
                {
                    if (Reference != null)
                    {
                        Reference[field] = (float)Math.Round(value, 3);
                    }
                    if (References?.ContainsKey(PrimaryReferenceFrame) == true)
                    {
                        References[PrimaryReferenceFrame][field] = (float)Math.Round(value, 3);
                    }
                }
                else if (IsLocked && CurrentFrame == EndFrame && EndFrame != PrimaryReferenceFrame)
                {
                    if (Reference != null && CurrentPoint != null)
                    {
                        if (CurrentPoint.State != PointState.Reference)
                        {
                            var reference = MakeReference(CurrentFrame);
                            if (reference != null)
                            {
                                CurrentPoint.State = PointState.Reference;
                                CurrentPoint.ReferenceFrame = CurrentFrame;
                                Reference = References[CurrentFrame] = reference;
                                GrayReference = null;
                                RgbReference = null;
                                GradientPoints = null;
                            }
                        }
                        Reference[field] = (float)Math.Round(value, 3);
                    }
                    if (References?.ContainsKey(CurrentFrame) == true)
                    {
                        References[CurrentFrame][field] = (float)Math.Round(value, 3);
                    }
                }
                UpdatePoint();
            }
        }

        #endregion
    }

    public enum TargetType { Static, Rotary, Dynamic }
}
