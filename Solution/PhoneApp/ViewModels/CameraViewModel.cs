using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MicroVue.Models;
using MicroVue.Views;
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

        string recordingDurationStr = "";
        public string RecordingDurationStr
        {
            get => recordingDurationStr;
            set
            {
                recordingDurationStr = value;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var seconds))
                {
                    SetRecordingDuration(seconds);
                }
                else
                {
                    OnPropertyChanged(nameof(RecordingDurationStr));
                    OnPropertyChanged(nameof(RecordingDurationSlider));
                }
            }
        }

        public double RecordingDurationSlider
        {
            get => Math.Clamp(Camera?.RecordingDuration ?? 0, 1, 10);
            set
            {
                if (value == RecordingDurationSlider) return;
                SetRecordingDuration(Math.Round(value), syncText: true);
            }
        }

        void SetRecordingDuration(double seconds, bool syncText = false)
        {
            if (Camera != null) Camera.RecordingDuration = seconds;
            if (syncText) recordingDurationStr = seconds.ToString("0", CultureInfo.CurrentCulture);
            OnPropertyChanged(nameof(RecordingDurationStr));
            OnPropertyChanged(nameof(RecordingDurationSlider));
        }

        public CameraViewModel()
        {
#if ANDROID
            Camera = new AndroidCamera();
#elif WINDOWS

#elif IOS
            Camera = new IOSCamera();
#endif
            
            if (Camera != null)
            {
                Camera.RecordingSaved += OnRecordingSaved;
                SetRecordingDuration(5, syncText: true);
            }
        }

        void OnRecordingSaved(string videoPath)
        {
            var scenePath = SaveScene(videoPath);
            if (scenePath == null) return;

            var sceneItem = new SceneItem
            {
                Name = Path.GetFileName(scenePath),
                Date = File.GetCreationTime(scenePath),
                ItemPath = scenePath,
            };

            try
            {
                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync(nameof(AnalysisPage),
                        new Dictionary<string, object> { { "SceneItem", sceneItem } });
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error opening recorded scene: {e}");
            }
        }

        string? SaveScene(string videoPath)
        {
            try
            {
                var baseName = Path.GetFileNameWithoutExtension(videoPath);
                var sceneName = baseName;
                var scenePath = App.DataFolder + sceneName;
                int n = 2;
                while (File.Exists(scenePath))
                {
                    sceneName = $"{baseName}_{n++}";
                    scenePath = App.DataFolder + sceneName;
                }

                var scene = new Scene { Name = sceneName, VideoName = videoPath };
                scene.Save(scenePath);
                return scenePath;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error saving recorded scene: {e}");
                return null;
            }
        }

        public void Initialize()
        {
            Camera?.Open(CameraFacing.Back);
        }

        #region Status

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
                Camera.StopRecording(true); // discard video if stopped early
            }
            else
            {
                var file = $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                Camera.StartRecording(App.VideoFolder + file);
            }
        }

        #endregion
    }
}