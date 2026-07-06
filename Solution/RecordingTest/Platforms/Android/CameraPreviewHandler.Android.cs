using Android.Content;
using Android.Graphics;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RecordingTest.Models;
using RecordingTest.Views;

namespace RecordingTest.Handlers
{
    public partial class CameraPreviewHandler
    {
        TextureView? textureView;
        SurfaceTexture? surfaceTexture;
        Surface? surface;
        int viewWidth, viewHeight;

        protected override TextureView CreatePlatformView()
        {
            var texture = new TextureView(Context);
            textureView = texture;
            texture.SurfaceTextureListener = new SurfaceListener(this);
            return texture;
        }

        // Re-runs whenever the bound Camera changes; reconnects if the surface is already live.
        static void MapCamera(CameraPreviewHandler handler, CameraPreview view) => handler.Attach();

        void Attach()
        {
            if (surfaceTexture == null || VirtualView?.Camera is not AndroidCamera cam) return;

            var size = cam.PreviewSize; // a real camera output size, not the view size
            ConfigureTransform(size.Width, size.Height);
            cam.SetPreviewTexture(surfaceTexture);
        }

        void OnSurfaceAvailable(SurfaceTexture st, int width, int height)
        {
            surfaceTexture = st;
            viewWidth = width; viewHeight = height;
            Attach();
        }

        void OnSurfaceSizeChanged(int width, int height)
        {
            viewWidth = width; viewHeight = height;
            if (VirtualView?.Camera is AndroidCamera cam)
                ConfigureTransform(cam.PreviewSize.Width, cam.PreviewSize.Height);
        }

        // scale down the image instead of stretching it
        void ConfigureTransform(int bufW, int bufH)
        {
            if (textureView == null || viewWidth == 0 || viewHeight == 0) return;

            // rotate to device position
            float contentAspect = (float)bufH / bufW;
            float viewAspect = (float)viewWidth / viewHeight;

            var m = new Matrix();
            if (viewAspect > contentAspect)
                m.SetScale(contentAspect / viewAspect, 1f, viewWidth / 2f, viewHeight / 2f);
            else
                m.SetScale(1f, viewAspect / contentAspect, viewWidth / 2f, viewHeight / 2f);
            textureView.SetTransform(m);
        }

        void OnSurfaceGone()
        {
            (VirtualView?.Camera as AndroidCamera)?.SetPreviewTexture(null);
            surfaceTexture = null;
        }

        // Android's listener must be a Java object, which a MAUI handler isn't - so bridge through a
        // small nested Java.Lang.Object.
        class SurfaceListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
        {
            readonly CameraPreviewHandler owner;
            public SurfaceListener(CameraPreviewHandler owner) => this.owner = owner;

            public void OnSurfaceTextureAvailable(SurfaceTexture st, int w, int h) => owner.OnSurfaceAvailable(st, w, h);
            public bool OnSurfaceTextureDestroyed(SurfaceTexture st) { owner.OnSurfaceGone(); return true; }
            public void OnSurfaceTextureSizeChanged(SurfaceTexture st, int w, int h) => owner.OnSurfaceSizeChanged(w, h);
            public void OnSurfaceTextureUpdated(SurfaceTexture st) { }
        }
    }

}
