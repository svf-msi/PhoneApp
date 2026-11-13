using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibSense.Models
{
    public enum Direction { Magnitude, X, Y, Z }

    public enum DataType { Displacement, Velocity, Acceleration }

    public enum Coordinates { Internal, Global }

    public enum ChartType { Linear, Logarithmic }
}
