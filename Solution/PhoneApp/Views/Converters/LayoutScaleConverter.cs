using MicroVue.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Views
{
    public class LayoutScaleConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Rect rect && parameter is ContentView cv && cv.BindingContext is AnalysisViewModel vm)
            {
                var scale = vm.PlayerScale;
                var newrect = new Rect(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
                //Debug.WriteLine($"[Debug]: convert scale={scale} - {rect}, {newrect}");
                return newrect;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Rect rect && parameter is ContentView cv && cv.BindingContext is AnalysisViewModel vm)
            {
                var scale = vm.PlayerScale;
                var newrect = new Rect(rect.X / scale, rect.Y / scale, rect.Width / scale, rect.Height / scale);
                return newrect;
            }
            return value;
        }
    }
}
