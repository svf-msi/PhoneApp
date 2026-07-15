using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MicroVue.Models;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace MicroVue.ViewModels
{
    public partial class CameraViewModel : ObservableObject
    {
        #region Fields and Properties

        [ObservableProperty]
        private ICameraService camera;

        [ObservableProperty]
        private CameraFacing facing;

        [ObservableProperty]
        private string recordingDurationStr = "0.0";
        partial void OnRecordingDurationStrChanged(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var d))
                Camera.RecordingDuration = d;
        }

        public CameraViewModel()
        {
#if ANDROID
            Camera = new AndroidCamera();
#elif WINDOWS

#elif IOS
            Camera = new IOSCamera();
#endif
        }

        public void Initialize()
        {
            Camera?.Open(CameraFacing.Back);
        }

        #region Settings

        public CameraCapabilities Capabilities => camera.Capabilities;

        public RangeInfo FrameRateRange => Capabilities.FrameRateRange;
        public RangeInfo ExposureRange => Capabilities.ExposureRange;
        public RangeInfo GainRange => Capabilities.GainRange;

        public bool SupportsManualExposure => Capabilities?.SupportsManualExposure ?? false;
        public bool SupportsManualGain => Capabilities?.SupportsManualGain ?? false;

        public double FrameRate
        {
            get => camera.FrameRate;
            set
            {
                camera.FrameRate = value;
                OnPropertyChanged();
            }
        }

        public double Exposure
        {
            get => camera.Exposure;
            set
            {
                camera.Exposure = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExposureDisplay));
            }
        }
        public string ExposureDisplay => $"{Exposure / 1000.0:0.0} ms";

        public double Gain
        {
            get => camera.Gain;
            set
            {
                camera.Gain = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GainDisplay));
            }
        }
        public string GainDisplay => $"ISO {Gain:0}";

        public bool AutoExposure
        {
            get => camera.AutoExposure;
            set
            {
                camera.AutoExposure = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ManualControlsEnabled));
            }
        }
        public bool ManualControlsEnabled => !AutoExposure;

        #endregion

        #region Status

        bool isOpen;
        public bool IsOpen
        {
            get => isOpen;
            private set { isOpen = value; OnPropertyChanged(); }
        }

        string status = "Idle";
        public string Status { get => status; private set { status = value; OnPropertyChanged(); } }

        [ObservableProperty]
        private bool settingsOpen;

        #endregion

        #endregion

        #region Auto-wired

        [RelayCommand]
        void SwitchFacing(object parameter)
        {
            Camera.SwitchFacing();
        }

        [RelayCommand]
        void ToggleSettingsOpen(object parameter)
        {
            SettingsOpen = !SettingsOpen;
        }

        [RelayCommand]
        void Record(object parameter)
        {
            if (Camera.IsRecording)
            {
                Camera.StopRecording(true);
            }
            else
            {
                Camera.StartRecording();
            }
        }

        #endregion
    }
}