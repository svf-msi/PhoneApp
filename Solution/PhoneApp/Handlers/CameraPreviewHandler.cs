using Microsoft.Maui.Handlers;
using MicroVue.Views;

// The native view type differs per platform; aliasing it lets this shared partial declare the
// ViewHandler base once. Each platform file below provides CreatePlatformView + MapCamera.
#if ANDROID
using PlatformView = Android.Views.TextureView;
#elif IOS
using PlatformView = UIKit.UIView;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.Controls.Image;
#else
using PlatformView = System.Object;
#endif

namespace MicroVue.Handlers
{
    public partial class CameraPreviewHandler : ViewHandler<CameraPreview, PlatformView>
    {
        public static readonly IPropertyMapper<CameraPreview, CameraPreviewHandler> PropertyMapper =
            new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewMapper)
            {
                [nameof(CameraPreview.Camera)] = MapCamera,
            };

        public CameraPreviewHandler() : base(PropertyMapper) { }

#if !ANDROID && !IOS && !WINDOWS
        protected override PlatformView CreatePlatformView() => new PlatformView();
        static void MapCamera(CameraPreviewHandler handler, CameraPreview view) { }
#endif
    }
}
