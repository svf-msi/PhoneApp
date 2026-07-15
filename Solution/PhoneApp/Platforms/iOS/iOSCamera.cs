using System;
using System.Linq;
using System.Threading.Tasks;
using AVFoundation;
using CoreFoundation;
using CoreMedia;
using Foundation;
using Photos;
using UIKit;
using MicroVue.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MicroVue.Models
{
    public partial class IOSCamera : ObservableObject, ICameraService
    {
        #region Fields

        AVCaptureDevice? device;
        AVCaptureSession? session;
        AVCaptureDeviceInput? input;
        AVCaptureMovieFileOutput? movieOutput;

        readonly DispatchQueue cameraQueue = new DispatchQueue("camera-bg");

        RecordingDelegate? recordingDelegate;
        NSUrl? outputUrl;
        bool discardRequested;

        int videoWidth = 1920, videoHeight = 1080;
        int recordFps;

        public AVCaptureSession? Session => session;

        public event Action? SessionConfigured;

        #endregion

        #region Bindable state

        [ObservableProperty]
        private bool isRecording;

        [ObservableProperty]
        private CameraCapabilities? capabilities;
        public CameraFacing Facing { get; set; }

        [ObservableProperty]
        private double frameRate;
        partial void OnFrameRateChanged(double value) => ApplyToDevice();

        [ObservableProperty]
        private double exposure;
        partial void OnExposureChanged(double value) => ApplyToDevice();

        [ObservableProperty]
        private double gain;
        partial void OnGainChanged(double value) => ApplyToDevice();

        [ObservableProperty]
        private bool autoExposure;
        partial void OnAutoExposureChanged(bool value) => ApplyToDevice();

        [ObservableProperty]
        private double recordingDuration;

        #endregion

        #region Opening and capabilities

        public async Task<bool> Open(CameraFacing facing = CameraFacing.Back)
        {
            var auth = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);
            if (auth == AVAuthorizationStatus.NotDetermined)
            {
                // ask for permission if we dont have it yet
                if (!await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video)) return false;
            }
            else if (auth != AVAuthorizationStatus.Authorized) return false;

            var tcs = new TaskCompletionSource<bool>();
            cameraQueue.DispatchAsync(() =>
            {
                try
                {
                    var dev = SelectDevice(facing);
                    if (dev == null)
                    {
                        Console.WriteLine("No matching iOS camera");
                        tcs.TrySetResult(false);
                        return;
                    }

                    device = dev;
                    Facing = facing;

                    ReadCapabilities(dev);
                    Console.WriteLine($"Camera capabilities:\n{Capabilities}");

                    SetSettingsFields(
                        ae: true,
                        fps: Capabilities!.FrameRateRange.Default,
                        exposureUs: Capabilities.ExposureRange.Default,
                        iso: Capabilities.GainRange.Default);

                    session = new AVCaptureSession();
                    session.BeginConfiguration();
                    session.SessionPreset = AVCaptureSession.PresetInputPriority;

                    input = AVCaptureDeviceInput.FromDevice(dev, out var inErr);
                    if (input == null || !session.CanAddInput(input))
                    {
                        Console.WriteLine($"Cannot add camera input: {inErr?.LocalizedDescription}");
                        session.CommitConfiguration();
                        tcs.TrySetResult(false);
                        return;
                    }
                    session.AddInput(input);

                    movieOutput = new AVCaptureMovieFileOutput();
                    if (session.CanAddOutput(movieOutput))
                        session.AddOutput(movieOutput);

                    session.CommitConfiguration();

                    ApplyToDeviceCore(); // apply default fps/exposure
                    session.StartRunning();

                    MainThread.BeginInvokeOnMainThread(() => SessionConfigured?.Invoke());
                    tcs.TrySetResult(true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error opening iOS camera: {e}");
                    tcs.TrySetResult(false);
                }
            });
            return await tcs.Task;
        }

        static AVCaptureDevice? SelectDevice(CameraFacing facing)
        {
            var pos = facing == CameraFacing.Front
                ? AVCaptureDevicePosition.Front
                : AVCaptureDevicePosition.Back;

            var discovery = AVCaptureDeviceDiscoverySession.Create(
                new[] { AVCaptureDeviceType.BuiltInWideAngleCamera },
                AVMediaTypes.Video,
                pos);

            return discovery.Devices.FirstOrDefault()
                   ?? AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
        }

        void ReadCapabilities(AVCaptureDevice dev)
        {
            var caps = new CameraCapabilities();

            double maxFps = 30;
            var rates = new System.Collections.Generic.SortedSet<double>();

            AVCaptureDeviceFormat? bestVideoFormat = null;
            long bestArea = -1;

            foreach (var f in dev.Formats)
            {
                var dims = Dimensions(f);
                long area = (long)dims.Width * dims.Height;

                foreach (var r in f.VideoSupportedFrameRateRanges)
                {
                    double fr = r.MaxFrameRate;
                    rates.Add(Math.Round(fr));
                    if (fr > maxFps) maxFps = fr;
                }

                bool supports30 = f.VideoSupportedFrameRateRanges.Any(r => r.MinFrameRate <= 30 && 30 <= r.MaxFrameRate);
                if (supports30 && area > bestArea)
                {
                    bestArea = area;
                    bestVideoFormat = f;
                    videoWidth = dims.Width;
                    videoHeight = dims.Height;
                }
            }
            double defaultFps = rates.Contains(120) ? 120 : Math.Min(30, maxFps);
            caps.FrameRateRange = new RangeInfo(1, maxFps, 1, defaultFps);
            caps.FrameRates = rates.ToList();

            var probe = bestVideoFormat ?? dev.ActiveFormat;

            // gain / iso
            caps.GainRange = new RangeInfo(probe.MinISO, probe.MaxISO, 1, probe.MinISO, dev.IsExposureModeSupported(AVCaptureExposureMode.Custom));

            // exposure (seconds -> microseconds)
            double minUs = probe.MinExposureDuration.Seconds * 1_000_000.0;
            double maxUs = probe.MaxExposureDuration.Seconds * 1_000_000.0;
            double minFrameInterval = 1_000_000.0 / caps.FrameRateRange.Max;
            caps.ExposureRange = new RangeInfo(minUs, Math.Min(maxUs, minFrameInterval), 0, minUs, dev.IsExposureModeSupported(AVCaptureExposureMode.Custom));

            Capabilities = caps;
        }

        static CMVideoDimensions Dimensions(AVCaptureDeviceFormat f)
        {
            return ((CMVideoFormatDescription)f.FormatDescription).Dimensions;
        }

        #endregion

        #region Parameter control

        void SetSettingsFields(bool ae, double fps, double exposureUs, double iso)
        {
            autoExposure = ae;
            frameRate = fps;
            exposure = exposureUs;
            gain = iso;
            OnPropertyChanged(nameof(AutoExposure));
            OnPropertyChanged(nameof(FrameRate));
            OnPropertyChanged(nameof(Exposure));
            OnPropertyChanged(nameof(Gain));
        }

        // Public entry point from the bindable setters - just hop onto the camera queue.
        void ApplyToDevice() => cameraQueue.DispatchAsync(ApplyToDeviceCore);

        void ApplyToDeviceCore()
        {
            if (device == null || Capabilities == null || session == null) return;
            if (movieOutput?.Recording == true) return; // don't reshape the format mid-recording

            if (!device.LockForConfiguration(out var err))
            {
                Console.WriteLine($"LockForConfiguration failed: {err?.LocalizedDescription}");
                return;
            }
            try
            {
                double fps = Capabilities.FrameRateRange.Clamp(FrameRate);

                var fmt = PickFormat(fps);
                if (fmt != null && fmt != device.ActiveFormat)
                {
                    device.ActiveFormat = fmt;
                    var dims = Dimensions(fmt);
                    videoWidth = dims.Width; videoHeight = dims.Height;
                }

                var ranges = device.ActiveFormat.VideoSupportedFrameRateRanges;
                fps = Math.Clamp(fps, ranges.Min(r => r.MinFrameRate), ranges.Max(r => r.MaxFrameRate));

                var frameDur = new CMTime(1, (int)Math.Max(1, Math.Round(fps)));
                device.ActiveVideoMinFrameDuration = frameDur;
                device.ActiveVideoMaxFrameDuration = frameDur;

                // setting activeformat can reset exposure
                if (AutoExposure)
                {
                    if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
                        device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                }
                else if (device.IsExposureModeSupported(AVCaptureExposureMode.Custom))
                {
                    double exposureUs = Capabilities.ExposureRange.Clamp(Exposure);
                    var dur = ClampDuration(CMTime.FromSeconds(exposureUs / 1_000_000.0, 1_000_000_000),
                                            device.ActiveFormat.MinExposureDuration,
                                            device.ActiveFormat.MaxExposureDuration);

                    double isoD = Capabilities.GainRange.Clamp(Gain);
                    float iso = (float)Math.Clamp(isoD, device.ActiveFormat.MinISO, device.ActiveFormat.MaxISO);

                    device.LockExposure(dur, iso, null);
                }
            }
            catch (Exception e) { Console.WriteLine($"ApplyToDevice error: {e.Message}"); }
            finally { device.UnlockForConfiguration(); }
        }

        AVCaptureDeviceFormat? PickFormat(double fps)
        {
            if (device == null) return null;
            AVCaptureDeviceFormat? best = null;
            long bestArea = -1;

            foreach (var f in device.Formats)
            {
                bool supportsFps = f.VideoSupportedFrameRateRanges
                    .Any(r => r.MinFrameRate - 0.1 <= fps && fps <= r.MaxFrameRate + 0.1);
                if (!supportsFps) continue;

                var dims = Dimensions(f);
                long area = (long)dims.Width * dims.Height;
                if (area > bestArea) { bestArea = area; best = f; }
            }
            return best;
        }

        static CMTime ClampDuration(CMTime value, CMTime min, CMTime max)
        {
            if (CMTime.Compare(value, min) < 0) return min;
            if (CMTime.Compare(value, max) > 0) return max;
            return value;
        }

        public Task SwitchFacing()
        {
            var next = Facing == CameraFacing.Back ? CameraFacing.Front : CameraFacing.Back;

            cameraQueue.DispatchAsync(() =>
            {
                if (session == null) return;
                var dev = SelectDevice(next);
                if (dev == null) return;

                session.BeginConfiguration();
                if (input != null) session.RemoveInput(input);

                var newInput = AVCaptureDeviceInput.FromDevice(dev, out var e);
                if (newInput != null && session.CanAddInput(newInput))
                {
                    session.AddInput(newInput);
                    input = newInput;
                    device = dev;
                    Facing = next;
                }
                else if (input != null)
                {
                    session.AddInput(input); // rollback
                }
                session.CommitConfiguration();

                ReadCapabilities(device!);
                SetSettingsFields(
                    ae: true,
                    fps: Capabilities!.FrameRateRange.Default,
                    exposureUs: Capabilities.ExposureRange.Default,
                    iso: Capabilities.GainRange.Default);
                ApplyToDeviceCore();
            });
            return Task.CompletedTask;
        }

        // closes the entire camera
        public void Close()
        {
            cameraQueue.DispatchAsync(() =>
            {
                try { if (session?.Running == true) session.StopRunning(); } catch { }
                try { if (input != null) session?.RemoveInput(input); } catch { }
                try { if (movieOutput != null) session?.RemoveOutput(movieOutput); } catch { }

                movieOutput?.Dispose(); movieOutput = null;
                input?.Dispose(); input = null;
                session?.Dispose(); session = null;
                device = null;
            });
        }

        #endregion

        #region Recording

        public void StartRecording()
        {
            cameraQueue.DispatchAsync(() =>
            {
                if (device == null || session == null || movieOutput == null) return;
                if (IsRecording || movieOutput.Recording) return;

                discardRequested = false;
                try
                {
                    // make sure the current fps/exposure/format is applied (also picks a high-speed format when the selected frame rate needs one).
                    int targetFps = (int)Math.Round(FrameRate);
                    recordFps = FrameRate > 30 ? targetFps : 0;
                    ApplyToDeviceCore();

                    var connection = movieOutput.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant());
                    if (connection != null)
                    {
                        if (connection.SupportsVideoOrientation)
                            connection.VideoOrientation = CurrentVideoOrientation();

                        TrySetBitrate(connection);
                    }

                    movieOutput.MaxRecordedDuration = RecordingDuration > 0
                        ? CMTime.FromSeconds(RecordingDuration, 600)
                        : CMTime.PositiveInfinity; // 0 = unlimited

                    var tmp = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(),
                        $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.mov");
                    outputUrl = NSUrl.FromFilename(tmp);

                    recordingDelegate ??= new RecordingDelegate(this);
                    movieOutput.StartRecordingToOutputFile(outputUrl, recordingDelegate);
                    IsRecording = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error starting recording: {e}");
                    IsRecording = false;
                }
            });
        }

        public void StopRecording(bool discard)
        {
            cameraQueue.DispatchAsync(() =>
            {
                if (movieOutput?.Recording != true) return;
                discardRequested = discard;
                try { movieOutput.StopRecording(); }
                catch (Exception e) { Console.WriteLine($"StopRecording failed: {e}"); }
            });
        }

        void OnFinishedRecording(NSUrl url, NSError? error)
        {
            bool durationStop = error != null && error.Code == (long)AVError.MaximumDurationReached;
            bool success = error == null || durationStop;

            if (discardRequested || !success)
                DeleteTemp(url);
            else
                SaveToPhotos(url);

            MainThread.BeginInvokeOnMainThread(() => IsRecording = false);
        }

        void SaveToPhotos(NSUrl url)
        {
            PHPhotoLibrary.RequestAuthorization(PHAccessLevel.AddOnly, status =>
            {
                if (status != PHAuthorizationStatus.Authorized && status != PHAuthorizationStatus.Limited)
                {
                    DeleteTemp(url);
                    return;
                }

                PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(() =>
                {
                    var req = PHAssetCreationRequest.CreationRequestForAsset();
                    req.AddResource(PHAssetResourceType.Video, url, new PHAssetResourceCreationOptions { ShouldMoveFile = true }); // moves the temp file in
                },
                (ok, err) =>
                {
                    if (!ok)
                    {
                        Console.WriteLine($"Save to Photos failed: {err?.LocalizedDescription}");
                        DeleteTemp(url);
                    }
                });
            });
        }

        static void DeleteTemp(NSUrl url)
        {
            try
            {
                if (url?.Path != null && System.IO.File.Exists(url.Path))
                    System.IO.File.Delete(url.Path);
            }
            catch { }
        }

        void TrySetBitrate(AVCaptureConnection connection)
        {
            try
            {
                int fps = recordFps > 0 ? recordFps : (int)Math.Round(FrameRate);
                const double bitsPerPixel = 0.15;
                long bitRate = (long)(videoWidth * (double)videoHeight * fps * bitsPerPixel);
                bitRate = Math.Min(bitRate, 42_000_000);

                var compression = new NSMutableDictionary
                {
                    [AVVideo.AverageBitRateKey] = NSNumber.FromLong((nint)bitRate)
                };
                var settings = new NSMutableDictionary
                {
                    [AVVideo.CodecKey] = AVVideoCodecType.H264.GetConstant(),
                    [AVVideo.CompressionPropertiesKey] = compression
                };
                movieOutput!.SetOutputSettings(settings, connection);
            }
            catch (Exception e) { Console.WriteLine($"Bitrate config skipped: {e.Message}"); }
        }

        static AVCaptureVideoOrientation CurrentVideoOrientation()
        {
            switch (UIDevice.CurrentDevice.Orientation)
            {
                case UIDeviceOrientation.LandscapeLeft: return AVCaptureVideoOrientation.LandscapeRight;
                case UIDeviceOrientation.LandscapeRight: return AVCaptureVideoOrientation.LandscapeLeft;
                case UIDeviceOrientation.PortraitUpsideDown: return AVCaptureVideoOrientation.PortraitUpsideDown;
                default: return AVCaptureVideoOrientation.Portrait;
            }
        }

        #endregion

        #region Helper classes

        class RecordingDelegate : AVCaptureFileOutputRecordingDelegate
        {
            readonly IOSCamera cam;
            public RecordingDelegate(IOSCamera cam) => this.cam = cam;

            public override void FinishedRecording(
                AVCaptureFileOutput captureOutput, NSUrl outputFileUrl,
                NSObject[] connections, NSError error)
                => cam.OnFinishedRecording(outputFileUrl, error);
        }

        #endregion
    }
}