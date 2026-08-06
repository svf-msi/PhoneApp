using MicroVue.Models;
using MicroVue.ViewModels;
using System.Diagnostics;
using System.Globalization;

namespace MicroVue.Views
{
    public class LayoutRegionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Rect rect && parameter is ContentView cv && cv.BindingContext is AnalysisViewModel vm)
            {
                var width = vm.VideoWidth;
                var height = vm.VideoHeight;
                var rotation = vm.MediaRotation;
                var scale = vm.PlayerScale;
                var rotated = rect.Rotate(rotation, width, height);
                var newrect = new Rect(rotated.X * scale, rotated.Y * scale, rotated.Width * scale, rotated.Height * scale);
                //Debug.WriteLine($"[Debug]: convert rotation={rotation}, scale={scale} - {rect}, {newrect}");
                return newrect;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
