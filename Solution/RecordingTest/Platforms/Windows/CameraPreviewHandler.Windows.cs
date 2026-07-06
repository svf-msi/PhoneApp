using RecordingTest.Views;
using Microsoft.UI.Xaml.Controls;

namespace RecordingTest.Handlers
{
    public partial class CameraPreviewHandler
    {
        protected override Microsoft.UI.Xaml.Controls.Image CreatePlatformView() => new Microsoft.UI.Xaml.Controls.Image();
        static void MapCamera(CameraPreviewHandler handler, CameraPreview view) { }
    }
}