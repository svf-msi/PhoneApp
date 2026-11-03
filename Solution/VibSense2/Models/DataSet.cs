using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VibSense.Models
{
    public class DataSet
    {
        public string Name { get; set; }

        public Int64 TimeStamp { get; set; }

        public List<Vector3> Data { get; set; }
    }
}
