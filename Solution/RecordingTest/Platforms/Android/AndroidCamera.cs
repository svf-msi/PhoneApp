using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using ASize = Android.Util.Size;
using ARange = Android.Util.Range;
using RecordingTest.ViewModels;
using Android.Runtime;
using Android.Graphics;
using Android.Views;

namespace RecordingTest.Models
{
    public class AndroidCamera : BaseViewModel, ICameraService
    {
        #region Properties

        static double Num(Java.Lang.Object o) => ((Java.Lang.Number)o).DoubleValue();

        private CameraDevice? device;
        private CameraCaptureSession? session;
        private CaptureRequest.Builder? requestBuilder;

        private Android.Views.Surface? previewSurface;
        private SurfaceTexture? previewTexture;
        private Android.OS.HandlerThread? backgroundThread;
        private Android.OS.Handler? backgroundHandler;

        private Android.Media.MediaRecorder? mediaRecorder;
        private Android.Views.Surface? recorderSurface;
        private MediaStoreVideo? output;
        private ASize videoSize = new ASize(1920, 1080);
        ASize defaultVideoSize = new ASize(1920, 1080);
        private int sensorOrientation;
        public ASize PreviewSize { get; private set; } = new ASize(1920, 1080);
        StreamConfigurationMap? configMap;
        ARange[]? aeFpsRanges;

        private bool recorderStarted;
        private bool useHighSpeed;

        private bool isRecording;
        public bool IsRecording { get => isRecording; set { isRecording = value; NotifyPropertyChanged(); } }
        public CameraCapabilities? Capabilities { get; set; }

        private double frameRate;
        public double FrameRate { get => frameRate; set { frameRate = value; ApplyToBuilder(); NotifyPropertyChanged(); } }
        private double exposure;
        public double Exposure { get => exposure; set { exposure = value; ApplyToBuilder(); NotifyPropertyChanged(); } }
        private double gain;
        public double Gain { get => gain; set { gain = value; ApplyToBuilder(); NotifyPropertyChanged(); } }

        private bool autoExposure;
        public bool AutoExposure { get => autoExposure; set { autoExposure = value; ApplyToBuilder(); NotifyPropertyChanged(); } }
        public CameraFacing Facing { get; set; }

        private double recordingDuration;
        public double RecordingDuration { get => recordingDuration; set { if (recordingDuration == value) return; recordingDuration = value; NotifyPropertyChanged(); } }

        #endregion

        #region Opening and capabilities

        public async Task<bool> Open(CameraFacing facing = CameraFacing.Back)
        {
            try
            {
                CameraManager manager = (CameraManager)Android.App.Application.Context.GetSystemService(Context.CameraService);
                string? cameraId = SelectCameraId(manager, facing);
                Facing = facing;

                if (cameraId == null)
                {
                    Console.WriteLine($"No matching Android camera");
                    return false;
                }

                ReadCapabilities(manager.GetCameraCharacteristics(cameraId));

                Console.WriteLine($"Camera capabilities:\n{Capabilities}");

                SetSettingsFields(
                    ae: true,
                    fps: Capabilities.FrameRateRange.Default,
                    exposureUs: Capabilities.ExposureRange.Default,
                    iso: Capabilities.GainRange.Default);

                // start background thread so camera is nonblocking
                backgroundThread = new Android.OS.HandlerThread("camera-bg");
                backgroundThread.Start();
                backgroundHandler = new Android.OS.Handler(backgroundThread.Looper);

                var opened = new TaskCompletionSource<bool>();
                manager.OpenCamera(cameraId, new DeviceStateCallback(this, opened), backgroundHandler);
                return await opened.Task;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error opening Android camera: {e}");
                return false;
            }
        }

        static string? SelectCameraId(CameraManager manager, CameraFacing facing)
        {
            var want = facing == CameraFacing.Front ? (int)LensFacing.Front : (int)LensFacing.Back;

            string? firstMatch = null;
            foreach (var id in manager.GetCameraIdList())
            {
                var chars = manager.GetCameraCharacteristics(id);
                if (!(chars.Get(CameraCharacteristics.LensFacing) is Java.Lang.Integer lens) || lens.IntValue() != want)
                    continue;

                if (firstMatch == null) firstMatch = id; // backup if no high speed capable options

                var caps = chars.Get(CameraCharacteristics.RequestAvailableCapabilities)?.ToArray<int>() ?? Array.Empty<int>();
                Console.WriteLine($"Camera {id}, high speed: {caps.Contains((int)RequestAvailableCapabilities.ConstrainedHighSpeedVideo)}");
                if (caps.Contains((int)RequestAvailableCapabilities.ConstrainedHighSpeedVideo)) return id;
            }
            return firstMatch ?? manager.GetCameraIdList().FirstOrDefault();
        }

        void ReadCapabilities(CameraCharacteristics chars)
        {
            var caps = new CameraCapabilities();

            // camera orientation
            if (chars.Get(CameraCharacteristics.SensorOrientation) is Java.Lang.Integer so)
                sensorOrientation = so.IntValue();

            // gain/iso
            if (chars.Get(CameraCharacteristics.SensorInfoSensitivityRange) is ARange iso)
            {
                caps.GainRange = new RangeInfo(Num(iso.Lower), Num(iso.Upper), 1, Num(iso.Lower));
            }

            // frame rates and resolutions
            if (chars.Get(CameraCharacteristics.ScalerStreamConfigurationMap) is StreamConfigurationMap map)
            {
                configMap = map;
                var previewClass = Java.Lang.Class.FromType(typeof(Android.Graphics.SurfaceTexture));
                double maxFps = 30;
                var sizes = map.GetOutputSizes(previewClass);
                if (sizes != null && sizes.Length > 0)
                {
                    var largest = sizes.OrderByDescending(s => (long)s.Width * s.Height).First();
                    long minDurationNs = map.GetOutputMinFrameDuration(previewClass, largest);
                    if (minDurationNs > 0) maxFps = 1_000_000_000.0 / minDurationNs;

                    PreviewSize = sizes
                        .Where(s => (long)s.Width * s.Height <= 1920L * 1080)
                        .OrderByDescending(s => (long)s.Width * s.Height)
                        .FirstOrDefault() ?? sizes[0];
                }
                caps.FrameRateRange = new RangeInfo(1, maxFps, 1, 30);

                var rates = new SortedSet<double>();
                aeFpsRanges = chars.Get(CameraCharacteristics.ControlAeAvailableTargetFpsRanges)?.ToArray<ARange>();
                Console.WriteLine($"FPS ranges #: {aeFpsRanges?.Length}");
                if (aeFpsRanges != null)
                {
                    foreach (var r in aeFpsRanges)
                    {
                        Console.WriteLine($"FPS option: {Num(r.Lower)}-{Num(r.Upper)}");
                        rates.Add(Num(r.Upper));
                    }
                }
                caps.FrameRates = rates.ToList();

                List<Resolution> highSpeedModes = new List<Resolution>();
                var hsSizes = map.GetHighSpeedVideoSizes();
                Console.WriteLine($"High speed sizes #: {hsSizes?.Length}");
                if (hsSizes != null && hsSizes.Length > 0)
                {
                    foreach (var size in hsSizes)
                    {
                        foreach (var range in map.GetHighSpeedVideoFpsRangesFor(size))
                        {
                            Console.WriteLine($"High speed option: {size.Width}x{size.Height} {Num(range.Lower)}-{Num(range.Upper)}");
                            highSpeedModes.Add(new Resolution(size.Width, size.Height, (int)Num(range.Upper)));
                        }
                    }
                }
                caps.HighSpeedModes = highSpeedModes;

                var recordSizes = map.GetOutputSizes(Java.Lang.Class.FromType(typeof(Android.Media.MediaRecorder)));
                if (recordSizes != null && recordSizes.Length > 0)
                {
                    defaultVideoSize = recordSizes
                        .Where(s => (long)s.Width * s.Height <= 1920L * 1080)
                        .OrderByDescending(s => (long)s.Width * s.Height)
                        .FirstOrDefault() ?? recordSizes[0];
                }
            }

            // exposure
            if (chars.Get(CameraCharacteristics.SensorInfoExposureTimeRange) is ARange expNs)
            {
                var minUs = Num(expNs.Lower) / 1000.0; // convert from ns to us
                var maxUs = Num(expNs.Upper) / 1000.0;
                var minFrameInterval = 1000000.0 / caps.FrameRateRange.Max; // we don't need long exposure shots
                caps.ExposureRange = new RangeInfo(minUs, Math.Min(maxUs, minFrameInterval), 0, minUs);
            }

            Capabilities = caps;
        }

        #endregion

        #region Preview

        // start capturing preview session
        public void StartPreview()
        {
            if (device == null || previewSurface == null || session != null) return;
            try
            {
                useHighSpeed = false;
                previewTexture?.SetDefaultBufferSize(PreviewSize.Width, PreviewSize.Height);

                requestBuilder = device.CreateCaptureRequest(CameraTemplate.Preview);
                requestBuilder.AddTarget(previewSurface);
                device.CreateCaptureSession(new List<Android.Views.Surface> { previewSurface },
                    new SessionStateCallback(this, _ => ApplyToBuilder()), backgroundHandler);
            }
            catch (Exception e) { Console.WriteLine($"Error starting preview: {e.Message}"); }
        }

        public void SetPreviewTexture(SurfaceTexture? texture)
        {
            previewTexture = texture;
            CloseSession();
            previewSurface?.Release();
            previewSurface = null;

            if (texture == null) return;

            texture.SetDefaultBufferSize(PreviewSize.Width, PreviewSize.Height);
            previewSurface = new Surface(texture);
            StartPreview();
        }

        // closes entire camera
        public void Close()
        {
            CloseSession();
            try { device?.Close(); } catch { }
            device = null;
            requestBuilder = null;

            // kill bg thread
            try { backgroundThread?.QuitSafely(); } catch { }
            backgroundThread = null;
            backgroundHandler = null;
        }

        // stops capture session while keeping camera device open
        void CloseSession()
        {
            try { session?.StopRepeating(); } catch { }
            try { session?.Close(); } catch { }
            session = null;
        }

        #endregion

        #region Parameter control

        void SetSettingsFields(bool ae, double fps, double exposureUs, double iso)
        {
            autoExposure = ae;
            frameRate = fps;
            exposure = exposureUs;
            gain = iso;
            NotifyPropertyChanged(nameof(AutoExposure));
            NotifyPropertyChanged(nameof(FrameRate));
            NotifyPropertyChanged(nameof(Exposure));
            NotifyPropertyChanged(nameof(Gain));
        }

        public async Task SwitchFacing()
        {
            var next = Facing == CameraFacing.Back ? CameraFacing.Front : CameraFacing.Back;

            Console.WriteLine($"Switching camera facing to {next}");

            Close();
            await Open(next);
        }

        void ApplyToBuilder(CameraCaptureSession.CaptureCallback? callback = null)
        {
            if (requestBuilder == null || Capabilities == null) return;
            if (useHighSpeed) return; // cannot change params in high speed mode

            requestBuilder.Set(CaptureRequest.ControlMode, (int)ControlMode.Auto);
            requestBuilder.Set(CaptureRequest.ControlAeMode,
                (int)(AutoExposure ? ControlAEMode.On : ControlAEMode.Off));

            if (!AutoExposure)
            {
                var exposureNs = (long)(Capabilities.ExposureRange.Clamp(Exposure) * 1000);
                var iso = (int)Capabilities.GainRange.Clamp(Gain);
                var fps = Capabilities.FrameRateRange.Clamp(FrameRate);

                requestBuilder.Set(CaptureRequest.SensorExposureTime, (Java.Lang.Long)exposureNs);
                requestBuilder.Set(CaptureRequest.SensorSensitivity, (Java.Lang.Integer)iso);

                long frameDurationNs = Math.Max((long)(1_000_000_000L / Math.Max(1, fps)), exposureNs);
                requestBuilder.Set(CaptureRequest.SensorFrameDuration, (Java.Lang.Long)frameDurationNs);
            }
            else
            {
                int fps = (int)Math.Round(Capabilities.FrameRateRange.Clamp(FrameRate));
                var range = aeFpsRanges?
                    .Where(r => (int)Num(r.Upper) == fps)
                    .OrderByDescending(r => Num(r.Lower))
                    .FirstOrDefault();
                if (range != null) requestBuilder.Set(CaptureRequest.ControlAeTargetFpsRange, range);
            }

            try { session?.SetRepeatingRequest(requestBuilder.Build(), callback, backgroundHandler); }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        #endregion

        #region Recording

        int recordFps;
        public void StartRecording()
        {
            if (device == null || previewSurface == null || IsRecording) return;
            recorderStarted = false;
            try
            {
                CloseSession(); // tear down preview-only session so we can make recording + preview

                int targetFps = (int)Math.Round(FrameRate);
                var hsModes = Capabilities.HighSpeedModes
                    .Where(m => m.MaxFrameRate == targetFps)
                    .OrderByDescending(m => (long)m.Width * m.Height)
                    .ToList();
                useHighSpeed = FrameRate > 60 && hsModes.Count > 0;
                recordFps = useHighSpeed ? targetFps : 0;
                videoSize = useHighSpeed ? new ASize(hsModes[0].Width, hsModes[0].Height) : defaultVideoSize;
                if (useHighSpeed)
                    previewTexture?.SetDefaultBufferSize(videoSize.Width, videoSize.Height);

                SetupMediaRecorder();
                recorderSurface = mediaRecorder.Surface;

                requestBuilder = device.CreateCaptureRequest(CameraTemplate.Record);
                requestBuilder.AddTarget(previewSurface);
                requestBuilder.AddTarget(recorderSurface);

                bool waitForAe = AutoExposure || useHighSpeed;
                var onConfigured = (Action<CameraCaptureSession>)(s =>
                {
                    var aeCb = waitForAe ? new AeConvergeCallback(this) : null;

                    if (useHighSpeed && s is CameraConstrainedHighSpeedCaptureSession hs)
                    {
                        try
                        {
                            // high-speed sessions require burst submission
                            var burst = hs.CreateHighSpeedRequestList(requestBuilder.Build());
                            hs.SetRepeatingBurst(burst, aeCb, backgroundHandler);
                        }
                        catch (Exception e) { Console.WriteLine($"High-speed burst failed: {e}"); }
                    }
                    else
                    {
                        ApplyToBuilder(aeCb);
                    }

                    // When recording the auto-exposure retriggers, so we have to wait a bit.
                    if (waitForAe)
                        backgroundHandler?.PostDelayed(BeginRecorder, 1200); // fallback if AE never reports converged
                    else
                        BeginRecorder();
                });

                if (useHighSpeed)
                {
                    var ranges = configMap!.GetHighSpeedVideoFpsRangesFor(videoSize).Where(r => (int)Num(r.Upper) == targetFps).ToList();
                    var range = ranges.FirstOrDefault(r => (int)Num(r.Lower) == targetFps) ?? ranges.First();

                    requestBuilder.Set(CaptureRequest.ControlAeTargetFpsRange, range);
                    device.CreateConstrainedHighSpeedCaptureSession(
                        new List<Android.Views.Surface> { previewSurface, recorderSurface },
                        new SessionStateCallback(this, onConfigured), backgroundHandler);
                }
                else
                {
                    device.CreateCaptureSession(new List<Android.Views.Surface> { previewSurface, recorderSurface },
                        new SessionStateCallback(this, onConfigured), backgroundHandler);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error starting recording: {e}");
                CleanupRecorder();
                StartPreview();
            }
        }

        internal void BeginRecorder()
        {
            if (recorderStarted || mediaRecorder == null) return;
            recorderStarted = true;
            try
            {
                mediaRecorder.Start();
                IsRecording = true;
            }
            catch (Exception e) { Console.WriteLine($"MediaRecorder.Start failed: {e}"); }
        }

        // get recording surface, set fps and orientation
        void SetupMediaRecorder()
        {
            output = new MediaStoreVideo($"Capture_{DateTime.Now:yyyyMMdd_HHmmss}");
            mediaRecorder = new Android.Media.MediaRecorder();
            mediaRecorder.SetVideoSource(Android.Media.VideoSource.Surface);
            mediaRecorder.SetOutputFormat(Android.Media.OutputFormat.Mpeg4);
            mediaRecorder.SetOutputFile(output.Fd.FileDescriptor);

            double fps;
            if (recordFps > 0)
            {
                fps = recordFps; // high speed we just use the selected frame rate
            }
            else
            {
                fps = Capabilities.FrameRateRange.Clamp(FrameRate);
                if (!AutoExposure)
                {
                    double exposureUs = Capabilities.ExposureRange.Clamp(Exposure);
                    fps = Math.Min(fps, 1_000_000.0 / exposureUs);
                }
            }
            int intFps = (int)Math.Round(fps);
            const double bitsPerPixel = 0.15;
            long bitRate = (long)(videoSize.Width * (double)videoSize.Height * intFps * bitsPerPixel); // make sure bitrate is fine for hi and lo fps
            mediaRecorder.SetVideoEncodingBitRate((int)Math.Min(bitRate, 42_000_000));
            mediaRecorder.SetVideoFrameRate(intFps);

            mediaRecorder.SetVideoSize(videoSize.Width, videoSize.Height);
            mediaRecorder.SetVideoEncoder(Android.Media.VideoEncoder.H264);

            // Calculate orientation hint based on phone rotation and camera
            var wm = Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            int deviceRotation = wm.DefaultDisplay.Rotation switch
            {
                Android.Views.SurfaceOrientation.Rotation90 => 90,
                Android.Views.SurfaceOrientation.Rotation180 => 180,
                Android.Views.SurfaceOrientation.Rotation270 => 270,
                _ => 0
            };
            int sign = Facing == CameraFacing.Front ? -1 : 1;
            var orientationHint = (sensorOrientation - deviceRotation * sign + 360) % 360;
            mediaRecorder.SetOrientationHint(orientationHint);

            if (RecordingDuration > 0) // 0 = unlimited
            {
                mediaRecorder.SetMaxDuration((int)(RecordingDuration * 1000)); // set in ms
                mediaRecorder.Info += (_, e) =>
                {
                    if (e.What == Android.Media.MediaRecorderInfo.MaxDurationReached)
                        backgroundHandler?.Post(() => StopRecording(false));
                };
            }

            mediaRecorder.Prepare();
        }

        void CleanupRecorder()
        {
            try { mediaRecorder?.Reset(); } catch { }
            try { mediaRecorder?.Release(); } catch { }
            mediaRecorder = null;
            output?.Dispose(); // close the ParcelFileDescriptor so the file is flushed
            output = null;
            try { recorderSurface?.Release(); } catch { }
            recorderSurface = null;
        }

        public void StopRecording(bool discard)
        {
            if (!IsRecording) return;
            try
            {
                try { session?.StopRepeating(); } catch { }

                if (!discard)
                {
                    try { mediaRecorder?.Stop(); output?.Finish(); }
                    catch (Exception e) { Console.WriteLine($"MediaRecorder.Stop failed: {e}"); }
                }
            }
            finally
            {
                if (discard) output?.Delete();
                IsRecording = false;
                CloseSession();
                CleanupRecorder();
                StartPreview();
            }
        }

        #endregion

        #region Helper classes

        class DeviceStateCallback : CameraDevice.StateCallback
        {
            readonly AndroidCamera svc;
            readonly TaskCompletionSource<bool> opened;
            public DeviceStateCallback(AndroidCamera svc, TaskCompletionSource<bool> opened)
            { this.svc = svc; this.opened = opened; }

            public override void OnOpened(CameraDevice camera)
            {
                svc.device = camera;
                opened.TrySetResult(true);
                svc.StartPreview();
            }
            public override void OnDisconnected(CameraDevice camera)
            {
                camera.Close(); svc.device = null; opened.TrySetResult(false);
            }
            public override void OnError(CameraDevice camera, CameraError error)
            {
                camera.Close(); svc.device = null;
                Console.WriteLine($"Camera device error: {error}");
                opened.TrySetResult(false);

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    svc.Close();
                    await svc.Open(svc.Facing);
                });
            }
        }

        class SessionStateCallback : CameraCaptureSession.StateCallback
        {
            readonly AndroidCamera svc;
            readonly Action<CameraCaptureSession> onConfigured;
            public SessionStateCallback(AndroidCamera svc, Action<CameraCaptureSession> onConfigured)
            { this.svc = svc; this.onConfigured = onConfigured; }

            public override void OnConfigured(CameraCaptureSession s)
            {
                svc.session = s;
                onConfigured(s);
            }

            public override void OnConfigureFailed(CameraCaptureSession s)
                => Console.WriteLine("Session configuration failed");
        }

        class AeConvergeCallback : CameraCaptureSession.CaptureCallback
        {
            readonly AndroidCamera svc;
            bool done;
            public AeConvergeCallback(AndroidCamera svc) => this.svc = svc;

            public override void OnCaptureCompleted(
                CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                if (done) return;
                var ae = (result.Get(CaptureResult.ControlAeState) as Java.Lang.Integer)?.IntValue();
                if (ae == null
                    || ae == (int)ControlAEState.Converged
                    || ae == (int)ControlAEState.Locked
                    || ae == (int)ControlAEState.FlashRequired)
                {
                    done = true;
                    svc.BeginRecorder();
                }
            }
        }

        // pending mediastore (photos app) entry
        class MediaStoreVideo : IDisposable
        {
            public Android.OS.ParcelFileDescriptor Fd { get; }
            readonly Android.Net.Uri? uri;

            public MediaStoreVideo(string displayName)
            {
                var resolver = Android.App.Application.Context.ContentResolver;
                var values = new Android.Content.ContentValues();
                values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, displayName);
                values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "video/mp4");
                values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryMovies);
                values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 1);

                uri = resolver.Insert(Android.Provider.MediaStore.Video.Media.ExternalContentUri, values);
                Fd = resolver.OpenFileDescriptor(uri, "w");
            }

            public void Finish()
            {
                if (uri == null) return;
                var values = new Android.Content.ContentValues();
                values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 0);
                Android.App.Application.Context.ContentResolver.Update(uri, values, null, null);
            }

            public void Delete()
            {
                try { Fd?.Close(); } catch { }
                if (uri != null)
                {
                    try { Android.App.Application.Context.ContentResolver.Delete(uri, null, null); }
                    catch { }
                }
            }

            public void Dispose()
            {
                try { Fd?.Close(); } catch { }
            }
        }

        #endregion

    }
}