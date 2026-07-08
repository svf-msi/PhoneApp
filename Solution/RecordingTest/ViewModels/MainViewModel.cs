using RecordingTest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RecordingTest.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Fields and Properties

        private ICameraService camera;
        public ICameraService Camera { get => camera; set { camera = value; NotifyPropertyChanged(); } }

        private CameraFacing facing;
        public CameraFacing Facing { get => facing; set { facing = value; NotifyPropertyChanged(); } }

        private string recordingDurationStr = "0.0";
        public string RecordingDurationStr
        {
            get => recordingDurationStr;
            set
            {
                recordingDurationStr = value;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var d))
                    Camera.RecordingDuration = d;
                NotifyPropertyChanged(); 
            }
        }

        public MainViewModel()
        {
#if ANDROID
            Camera = new AndroidCamera();
#elif WINDOWS

#elif IOS

#endif

            Initialize();
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
                NotifyPropertyChanged();
            }
        }

        public double Exposure
        {
            get => camera.Exposure;
            set
            {
                camera.Exposure = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ExposureDisplay));
            }
        }
        public string ExposureDisplay => $"{Exposure / 1000.0:0.0} ms";

        public double Gain
        {
            get => camera.Gain;
            set
            {
                camera.Gain = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(GainDisplay));
            }
        }
        public string GainDisplay => $"ISO {Gain:0}";

        public bool AutoExposure
        {
            get => camera.AutoExposure;
            set
            {
                camera.AutoExposure = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ManualControlsEnabled));
            }
        }
        public bool ManualControlsEnabled => !AutoExposure;

        #endregion

        #region Status

        bool isOpen;
        public bool IsOpen
        {
            get => isOpen;
            private set { isOpen = value; NotifyPropertyChanged(); }
        }

        string status = "Idle";
        public string Status { get => status; private set { status = value; NotifyPropertyChanged(); } }

        private bool settingsOpen;
        public bool SettingsOpen { get => settingsOpen; set { settingsOpen = value; NotifyPropertyChanged(); } }

        #endregion

        #endregion

        #region Commands

        public ICommand RecordCommand { get; protected set; }
        public ICommand SwitchFacingCommand { get; protected set; }
        public ICommand ToggleSettingsOpenCommand { get; protected set; }

        #endregion

        #region Auto-wired

        void SwitchFacing(object parameter)
        {
            Camera.SwitchFacing();
        }

        void ToggleSettingsOpen(object parameter)
        {
            SettingsOpen = !SettingsOpen;
        }

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
