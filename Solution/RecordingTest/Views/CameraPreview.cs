using RecordingTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingTest.Views
{
    public class CameraPreview : View
    {
        public static readonly BindableProperty CameraProperty =
            BindableProperty.Create(nameof(Camera), typeof(ICameraService), typeof(CameraPreview));

        public ICameraService Camera
        {
            get => (ICameraService)GetValue(CameraProperty);
            set => SetValue(CameraProperty, value);
        }
    }

}
