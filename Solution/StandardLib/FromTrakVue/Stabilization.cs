using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardLib
{
    public class Stabilization
    {
        static MatrixBuilder<double> M = Matrix<double>.Build;
        static VectorBuilder<double> V = Vector<double>.Build;

        public static double CrossProduct(Vector<double> vector1, Vector<double> vector2)
        {
            return vector1.At(0) * vector2.At(1) - vector1.At(1) * vector2.At(0);
        }

        public List<TrackPoint> Points { get; set; }
        public List<TrackPoint> Priors { get; set; }

        public Vector<double> CG { get; set; }
        public Vector<double> PriorCG { get; set; }
        public Vector<double> Shift { get; set; }
        public Vector<double> Translation { get; set; }
        public double Angle { get; set; }
        public bool IsValid { get; set; }
        public Matrix<double> R
        {
            get
            {
                var cos = Math.Cos(Angle);
                var sin = Math.Sin(Angle);
                return M.Dense(2, 2, new double[] { cos, sin, -sin, cos });
            }
        }

        public Transform Transform => new Transform(Translation, Angle);

        public Stabilization(List<TrackPoint> points, List<TrackPoint> priors)
        {
            if (points?.Count == priors?.Count)
            {
                Points = points; Priors = priors;
                try
                {
                    GetCG();
                    GetAngle();
                    GetTranslation();
                    IsValid = Translation != null;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in stabilization: {e}");
                }
            }
        }

        public void GetCG()
        {
            var x = Points?.Select(p => (double)p.X).Average() ?? 0; 
            var y = Points?.Select(p => (double)p.Y).Average() ?? 0;
            CG = V.Dense(new double[] { x, y });
            x = Priors?.Select(p => (double)p.X).Average() ?? 0;
            y = Priors?.Select(p => (double)p.Y).Average() ?? 0;
            PriorCG = V.Dense(new double[] { x, y });
            Shift = CG - PriorCG;
            //Console.WriteLine($"CG: {CG.At(0)}, {CG.At(1)}, Prior CG: {PriorCG.At(0)}, {PriorCG.At(1)}, Shift: {Shift.At(0)}, {Shift.At(1)}");
        }

        public void GetAngle()
        {
            var angles = new List<double>();
            for (int i = 0; i < Points.Count; ++i)
            {
                var point = V.Dense(new double[] { Points[i].X, Points[i].Y });
                var prior = V.Dense(new double[] { Priors[i].X, Priors[i].Y });
                prior -= PriorCG;
                point -= CG;
                var angle = Math.Asin(CrossProduct(prior, point) / (point.L2Norm() * prior.L2Norm()));
                angles.Add(angle);
                //Console.WriteLine($"point: {point.At(0)}, {point.At(1)}, prior: {prior.At(0)}, {prior.At(1)}, angle: {angle}");
            }
            Angle = angles.Average();
        }

        public void GetTranslation()
        {
            // T = S + C - RC
            //Shift = CG - PriorCG;
            Translation = Shift + PriorCG - R.Multiply(PriorCG);
        }
    }
}
