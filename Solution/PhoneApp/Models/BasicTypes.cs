using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Models
{
    public class ChartDataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class LineSeriesModel
    {
        public string Name { get; set; } = "";
        public ObservableCollection<ChartDataPoint> Points { get; set; } = new ObservableCollection<ChartDataPoint>();
    }

    public enum DataParameter { X, Y, Magnitude }
}
