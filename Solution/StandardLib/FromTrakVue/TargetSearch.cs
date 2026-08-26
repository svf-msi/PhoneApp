using Emgu.CV;
using Emgu.CV.CvEnum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace StandardLib
{
    public class TargetSearch
    {
        public static TargetSearch Make<T>(Image<T, float> image, Image<T, float> referencePattern, TrackRegion region, double errorThreshold = 0)
            where T : struct, IColor
        {
            return new TargetSearch<T>(image, referencePattern, region, errorThreshold);
        }

        public static SearchDirection Opposite(SearchDirection direction)
        {
            switch (direction)
            {
                case SearchDirection.Up: return SearchDirection.Down;
                case SearchDirection.Down: return SearchDirection.Up;
                case SearchDirection.Right: return SearchDirection.Left;
                case SearchDirection.Left: return SearchDirection.Right;
                default: return direction;
            }
        }

        public static SearchDirection[] Directions = (SearchDirection[])Enum.GetValues(typeof(SearchDirection));
        public static int SearchLimit = 50;
        public static int SearchRange = 1;
        public static int MaxSearchRange = 4;
        public static ErrorType ErrorType = ErrorType.Abs;

        public bool IsValid { get; set; } = true;
        public bool IsComplete { get; set; } = false;
        public bool NotFound { get; set; } = false;
        public double[,] GlobalErrors { get; protected set; }
        public double MinGlobalError { get; protected set; }
        public double MaxGlobalError { get; protected set; }
        public double ErrorThreshold { get; set; }

        public virtual PointF Find(PatternSearchType type = PatternSearchType.Global)
        {
            return new PointF();
        }
    }

    public class TargetSearch<T> : TargetSearch
        where T : struct, IColor
    {
        float x, y;
        Dictionary<(int, int), double> cachedErrors { get; set; } = new Dictionary<(int, int), double>();

        public Image<T, float> Image { get; set; }
        public Image<T, float> ReferencePattern { get; set; }
        public TrackRegion Region { get; set; }
        public PointF Position { get; set; }
        public Point Shift = new Point(0, 0);

        public TargetSearch(Image<T, float> image, Image<T, float> referencePattern, TrackRegion region, double errorThreshold = 0)
        {
            Image = image;
            ReferencePattern = referencePattern;
            Region = region;
            ErrorThreshold = errorThreshold;
            Reset();
            var rectangle = Rectangle.Round(region.Rectangle);
            Position = new PointF { X = rectangle.X + (float)rectangle.Width / 2, Y = rectangle.Y + (float)rectangle.Height / 2 };
            //Console.WriteLine($"[Debug]: Search check: {image}, {referencePattern}, {Region.Rectangle}, {rectangle}");
            if (image == null || Region == null || !new Rectangle(0, 0, image.Width, image.Height).Contains(rectangle))
                IsValid = false;
        }

        public override PointF Find(PatternSearchType type = PatternSearchType.Global)
        {
            var count = 0;
            while (!IsComplete && !NotFound && count++ < SearchLimit)
            {
                if (type == PatternSearchType.Local)
                {
                    FindLocalErrors();
                }
                else
                {
                    FindGlobalErrors(SearchRange);
                }
            }

            return NotFound ? Position : Position + new SizeF(Shift.X, Shift.Y);
        }

        void FindGlobalErrors(int range = 1)
        {
            range = Math.Max(1, range);
            int c = 0, r = 0;
            int step = 1;
            var threshold = ErrorThreshold;
            MinGlobalError = double.PositiveInfinity;

            while (range <= MaxSearchRange)
            {
                MaxGlobalError = 0;
                var size = 2 * range + 1;
                GlobalErrors = new double[size, size];

                for (int col = -range; col <= range; col+=step)
                {
                    for (int row = -range; row <= range; row+=step)
                    {
                        var error = FindGlobalError(col + Shift.X, row + Shift.Y);
                        GlobalErrors[col + range, row + range] = error;
                        if (error < MinGlobalError)
                        {
                            MinGlobalError = error;
                            c = col; r = row;
                        }
                        if (error > MaxGlobalError)
                        {
                            MaxGlobalError = error;
                        }
                    }
                }

                //Debug.WriteLine($"[Debug]: Search: range={range}, step={step}, threshold={threshold}, minError={MinGlobalError}");
                if (threshold == 0 || MinGlobalError <= threshold) break;
                else
                {
                    threshold = threshold * 1.1; //ErrorThreshold * 1.1;
                    range *= 2; step = 1;
                }
            } 

            if (ErrorThreshold > 0 && MinGlobalError > threshold)
            {
                NotFound = true;
            }
            else
            {
                if (c == 0 && r == 0)
                {
                    IsComplete = true;
                    ErrorThreshold = UpdateErrorThreshold(2);
                }
                else
                {
                    Shift.X += c;
                    Shift.Y += r;
                }
            }
        }

        double FindGlobalError(int col, int row)
        {
            if (cachedErrors.ContainsKey((col, row))) return cachedErrors[(col, row)];
            
            double error = 0;
            var offsetRegion = Rectangle.Round(Region.Rectangle);
            offsetRegion.Offset(col, row);
            if (offsetRegion.Top < 0 || offsetRegion.Bottom > Image.Height ||
                offsetRegion.Left < 0 || offsetRegion.Right > Image.Width) return double.PositiveInfinity;

            using (var pattern = ImageAnalysis.PreparePattern(offsetRegion, Image)) // blur & normalize by default
            {
                error = CvInvoke.Norm(pattern, ReferencePattern, NormType.L1);
            }
            cachedErrors[(col, row)] = error;
            return error;
        }

        double UpdateErrorThreshold(int range = 1)
        {
            range = Math.Max(1, range);
            var newThreshold = 0.0;
            for (int col = -range; col <= range; ++col)
            {
                for (int row = -range; row <= range; ++row)
                {
                    var error = FindGlobalError(col + Shift.X, row + Shift.Y);
                    if (error > newThreshold)
                    {
                        newThreshold = error;
                    }
                }
            }
            //Debug.WriteLine($"[Debug]: update error threshold = {newThreshold}");
            return newThreshold;
        }

        // obsolete

        public Dictionary<SearchDirection, double?> Errors { get; set; } = new Dictionary<SearchDirection, double?>();

        void FindLocalErrors()
        {
            foreach (var direction in Directions)
            {
                FindLocalError(direction);
            }
            FindMinLocalError();
        }

        void FindLocalError(SearchDirection direction)
        {
            if (Errors[direction] == null)
            {
                var offsetRegion = Rectangle.Round(Region.Rectangle);
                offsetRegion.Offset(Shift);
                ShiftSearchRegion(ref offsetRegion, direction);
                if (offsetRegion.Top < 0 || offsetRegion.Bottom > Image.Height ||
                    offsetRegion.Left < 0 || offsetRegion.Right > Image.Width) return;

                using (var pattern = ImageAnalysis.PreparePattern(offsetRegion, Image))
                using (var diff = pattern.Sub(ReferencePattern))
                {
                    //var errors = diff.Convert((float f) => f * f).GetSum().MCvScalar;
                    var errors = diff.Convert((float f) => Math.Abs(f)).GetSum().MCvScalar;
                    Errors[direction] = errors.V0 + errors.V1 + errors.V2;
                }
            }
        }

        void FindMinLocalError()
        {
            double minimumError = double.PositiveInfinity;
            SearchDirection minimumDirection = SearchDirection.None;

            foreach (var direction in Directions)
            {
                if (Errors[direction] != null && Errors[direction] < minimumError)
                {
                    minimumError = (double)Errors[direction];
                    minimumDirection = direction;
                }
            }

            if (minimumDirection == SearchDirection.None)
            {
                IsComplete = true;
            }
            else
            {
                var newCenterError = minimumError;
                var currentCenterError = Errors[SearchDirection.None];
                Reset(newCenterError, currentCenterError, Opposite(minimumDirection));
                ShiftOffset(minimumDirection);
            }
        }

        void RefinePosition()
        {
            if (Errors[SearchDirection.None] == null) return;
            var center = Math.Sqrt((double)Errors[SearchDirection.None]);
            x = Shift.X; y = Shift.Y;

            if (Errors[SearchDirection.Left] != null && Errors[SearchDirection.Right] != null)
            {
                var left = Math.Sqrt((double)Errors[SearchDirection.Left]) - center;
                var right = Math.Sqrt((double)Errors[SearchDirection.Right]) - center;

                x += (float)((right - left) / 2 / Math.Max(left, right));
            }

            if (Errors[SearchDirection.Up] != null && Errors[SearchDirection.Down] != null)
            {
                var up = Math.Sqrt((double)Errors[SearchDirection.Up]) - center;
                var down = Math.Sqrt((double)Errors[SearchDirection.Down]) - center;

                y += (float)((down - up) / 2 / Math.Max(up, down));
            }
        }

        void Reset(double? centerError = null, double? edgeError = null, SearchDirection edgeDirection = SearchDirection.None)
        {
            Errors[SearchDirection.None] = centerError;
            Errors[SearchDirection.Up] = null;
            Errors[SearchDirection.Right] = null;
            Errors[SearchDirection.Down] = null;
            Errors[SearchDirection.Left] = null;
            if (edgeDirection != SearchDirection.None)
            {
                Errors[edgeDirection] = edgeError;
            }
        }

        void ShiftOffset(SearchDirection direction)
        {
            switch (direction)
            {
                case SearchDirection.Up:
                    Shift.Offset(0, -1);
                    break;
                case SearchDirection.Right:
                    Shift.Offset(1, 0);
                    break;
                case SearchDirection.Down:
                    Shift.Offset(0, 1);
                    break;
                case SearchDirection.Left:
                    Shift.Offset(-1, 0);
                    break;
            }
        }

        void ShiftSearchRegion(ref Rectangle region, SearchDirection direction)
        {
            switch (direction)
            {
                case SearchDirection.Up:
                    region.Offset(0, -1);
                    break;
                case SearchDirection.Right:
                    region.Offset(1, 0);
                    break;
                case SearchDirection.Down:
                    region.Offset(0, 1);
                    break;
                case SearchDirection.Left:
                    region.Offset(-1, 0);
                    break;
            }
        }

    }

    #region Supporting types

    public class TrackRegion
    {
        public string Name { get; set; }
        public int FrameNumber { get; set; }
        public DateTime Time { get; set; }
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Width { get; set; } = 0;
        public float Height { get; set; } = 0;

        [JsonIgnore]
        public System.Drawing.RectangleF Rectangle
        {
            get => new System.Drawing.RectangleF(X - Width / 2, Y - Height / 2, Width, Height);
            set { X = value.X + value.Width / 2; Y = value.Y + value.Height / 2; Width = value.Width; Height = value.Height; }
        }

        [JsonIgnore]
        public System.Drawing.PointF Position
        {
            get => new System.Drawing.PointF(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        [JsonIgnore]
        public TrackPoint TrackPoint => new TrackPoint
        {
            Frame = FrameNumber,
            ReferenceFrame = FrameNumber,
            Time = Time,
            State = PointState.Reference,
            X = X,
            Y = Y,
        };

        public float this[string field]
        {
            get
            {
                switch (field)
                {
                    case nameof(FrameNumber):
                        return FrameNumber;
                    case nameof(X):
                        return X;
                    case nameof(Y):
                        return Y;
                    case nameof(Width):
                        return Width;
                    case nameof(Height):
                        return Height;
                    default:
                        return float.NaN;
                }
            }
            set
            {
                switch (field)
                {
                    case nameof(X):
                        X = value;
                        break;
                    case nameof(Y):
                        Y = value;
                        break;
                    case nameof(Width):
                        Width = value;
                        break;
                    case nameof(Height):
                        Height = value;
                        break;
                    case nameof(FrameNumber):
                        FrameNumber = (int)value;
                        break;
                }
            }
        }
    }

    public class TrackPoint
    {
        public int Frame { get; set; }
        public int ReferenceFrame { get; set; }
        public DateTime Time { get; set; }
        public PointState State { get; set; } = PointState.None;
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Angle { get; set; } = 0;

        [JsonIgnore]
        public TransformPoint Point => new TransformPoint { X = X, Y = Y };

        [JsonIgnore]
        public double Magnitude { get => Math.Sqrt(X * X + Y * Y); }

        [JsonIgnore]
        public System.Drawing.PointF Position
        {
            get => new System.Drawing.PointF(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        public float this[string field]
        {
            get
            {
                switch (field)
                {
                    case nameof(X):
                        return X;
                    case nameof(Y):
                        return Y;
                    case nameof(Magnitude):
                        return (float)Magnitude;
                    case nameof(Angle):
                        return Angle;
                    default:
                        return float.NaN;
                }
            }
            set
            {
                switch (field)
                {
                    case nameof(X):
                        X = value;
                        break;
                    case nameof(Y):
                        Y = value;
                        break;
                    case nameof(Angle):
                        Angle = value;
                        break;
                }
            }
        }

        public TrackRegion MakeTrackRegion(float width, float height, float offsetX = 0, float offsetY = 0)
        {
            return new TrackRegion
            {
                FrameNumber = Frame,
                Time = Time,
                X = X + offsetX,
                Y = Y + offsetY,
                Width = width,
                Height = height
            };
        }

        public TrackPoint Offset(double x, double y)
        {
            var point = Copy();
            point.X -= (float)x;
            point.Y -= (float)y;
            return point;
        }

        public TrackPoint Copy() => MemberwiseClone() as TrackPoint;

        public override string ToString()
        {
            return $"Point: X={X}, Y={Y}, A={Angle}";
        }
    }

    public enum SearchDirection
    {
        None, Up, Right, Down, Left
    }

    public enum PatternSearchType
    {
        Local, Global
    }

    public enum ErrorType { Abs, Squares }

    public enum PointState { None, Reference, Manual, Auto }

    public enum DataDirection { X, Y, Magnitude }

    #endregion
}

