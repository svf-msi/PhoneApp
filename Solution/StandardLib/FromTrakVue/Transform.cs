using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardLib
{
    public class Transform
    {
        static MatrixBuilder<double> M = Matrix<double>.Build;
        static VectorBuilder<double> V = Vector<double>.Build;

        public static List<Transform> Create(List<double> xShifts, List<double> yShifts, List<double> angles)
        {
            if (angles?.Count != xShifts?.Count || angles?.Count != yShifts?.Count) return null;
            var transforms = new List<Transform>();
            for (int i = 0; i < angles.Count; ++i)
            {
                transforms.Add(new Transform(xShifts[i], yShifts[i], angles[i]));
            }
            return transforms;
        }

        public static List<Transform> Accumulate(IEnumerable<Transform> transforms)
        {
            if (transforms == null) return null;

            var globalAngle = 0.0;
            var displacement = V.Dense(new double[] { 0, 0 }); // global displacement
            var globalTransforms = new List<Transform>();

            foreach (var transform in transforms)
            {
                globalAngle += transform.Angle;
                var rot = transform.R;
                var tran = transform.T;
                displacement = transform.Forward(displacement);
                globalTransforms.Add(new Transform(displacement, globalAngle));
            }
            return globalTransforms;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }

        public Transform() { }

        public Transform(double x, double y, double angle)
        {
            X = x; Y = y; Angle = angle;
        }

        public Transform(Vector<double> displacement, double angle)
        {
            X = displacement.At(0); Y = displacement.At(1); Angle = angle;
        }

        public Transform(TrackPoint point)
        {
            X = point.X; Y = point.Y; Angle = point.Angle;
        }

        public Matrix<double> R
        {
            get
            {
                var cos = Math.Cos(Angle);
                var sin = Math.Sin(Angle);
                return M.Dense(2, 2, new double[] { cos, sin, -sin, cos });
            }
        }

        public Matrix<double> InvR
        {
            get
            {
                var cos = Math.Cos(Angle);
                var sin = Math.Sin(Angle);
                return M.Dense(2, 2, new double[] { cos, -sin, sin, cos });
            }
        }

        public Vector<double> T
        {
            get
            {
                return V.Dense(new double[] { X, Y });
            }
        }

        public Vector<double> InvT
        {
            get
            {
                return -InvR.Multiply(T);
            }
        }

        public Vector<double> Shift(Vector<double> shift)
        {
            return R.Multiply(shift) + T;
        }

        public Vector<double> Forward(Vector<double> point)
        {
            return R.Multiply(point) + T;
        }

        public Vector<double> Backward(Vector<double> point)
        {
            return InvR.Multiply(point) + InvT;
        }

        public TrackPoint Backward(TrackPoint point)
        {
            var newPoint = InvR.Multiply(V.Dense(new double[] { point.X, point.Y })) + InvT;
            return new TrackPoint()
            {
                Frame = point.Frame,
                X = (float)newPoint.At(0),
                Y = (float)newPoint.At(1),
                Angle = point.Angle - (float)Angle
            };
        }

        public override string ToString()
        {
            return $"T: A={Angle}, X={X}, Y={Y}";
        }

        public Transform Copy() => MemberwiseClone() as Transform;
    }
}
