using MicroVue.Models;
using MicroVue.ViewModels;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Views
{
    public class LayoutTargetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TrackRegion region && parameter is ContentView cv && cv.BindingContext is AnalysisViewModel vm)
            {
                var width = vm.VideoWidth;
                var height = vm.VideoHeight;
                var rotation = vm.MediaRotation;
                var scale = vm.PlayerScale;
                var rect = new Rect(region.X - region.Width / 2, region.Y - region.Height / 2, region.Width, region.Height);
                var rotated = rect.Rotate(rotation, width, height);
                var newrect = new Rect(rotated.X * scale, rotated.Y * scale, rotated.Width * scale, rotated.Height * scale);
                return newrect;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
