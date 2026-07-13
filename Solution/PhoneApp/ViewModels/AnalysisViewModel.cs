using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroVue.Models;
using Newtonsoft.Json;
using SkiaSharp;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace MicroVue.ViewModels
{
    [QueryProperty(nameof(SceneItem), "SceneItem")]
    public partial class AnalysisViewModel : ObservableObject
    {
        [ObservableProperty]
        List<string> targetColors = new List<string> { "Red", "Green", "Blue", "Yellow", "Teal", "Purple" };

        [ObservableProperty]
        SceneItem sceneItem = new SceneItem();

        [ObservableProperty]
        Scene scene;

        [ObservableProperty]
        string sceneName;

        [ObservableProperty]
        string videoPath;

        [ObservableProperty]
        private ObservableCollection<string> speeds = new() { "x1", "x2", "x10" };

        [ObservableProperty]
        private int selectedSpeedIndex = 0;

        [ObservableProperty]
        Models.Region selectedRegion;

        [ObservableProperty]
        bool isRegionSelected;

        [ObservableProperty]
        bool isPlaying = false;

        [ObservableProperty]
        bool onMainView = true;

        [ObservableProperty]
        bool onChartView;

        [ObservableProperty]
        bool onVideoView;

        [ObservableProperty]
        ImageSource image;

        [ObservableProperty]
        MediaSource source;

        [ObservableProperty]
        double defaultSize = 100;

        [ObservableProperty]
        double mediaWidth;

        [ObservableProperty]
        double mediaHeight;

        [ObservableProperty]
        double playerWidth;

        [ObservableProperty]
        double playerHeight;

        [ObservableProperty]
        double playerScale;

        [ObservableProperty]
        double videoWidth;

        [ObservableProperty]
        double videoHeight;

        [ObservableProperty]
        int currentFrame = 0;

        [ObservableProperty]
        bool isRotated;

        [ObservableProperty]
        bool back;

        Video_MP4 video;

        partial void OnSceneItemChanged(SceneItem sceneItem)
        {
            if (sceneItem != null)
            {
                SceneName = sceneItem.Name;
                var scenePath = sceneItem.ItemPath;
                Scene = Scene.Read(scenePath);
                if (Scene == null) return;
                VideoPath = Scene.VideoName;
                SetupSource();
                SetupVideo();
            }
        }

        partial void OnSelectedRegionChanged(MicroVue.Models.Region region)
        {
            IsRegionSelected = SelectedRegion != null;
        }

        void SetupVideo(bool useImage = false)
        {
            if (!string.IsNullOrEmpty(VideoPath))
            {
                video = new Video_MP4(VideoPath);
                CurrentFrame = 0;
                VideoWidth = video?.Width ?? 0;
                VideoHeight = video?.Height ?? 0;
                IsRotated = MediaWidth > 0 && VideoWidth > 0 && VideoWidth == MediaHeight;
                //if (useImage) SetImage();
            }
        }

        void SetupSource()
        {
            if (!string.IsNullOrEmpty(VideoPath))
            {
                Source = MediaSource.FromFile(VideoPath);
            }
        }

        void SetImage()
        {
            if (video == null) return;
            var bitmap = video.GetFrame(CurrentFrame);
            if (bitmap == null) return;

            using (var ms = new MemoryStream())
            {
                bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
                ms.Position = 0;
                Image = ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
            }
        }

        partial void OnBackChanged(bool value)
        {
            if (value) _ = GoBack();
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        void Delete(MicroVue.Models.Region region)
        {
            if (Scene?.Regions != null && region != null)
            {
                Scene.Regions.Remove(region);
                SelectedRegion = null;
            }
        }

        [RelayCommand]
        void AddTarget()
        {
            if (Scene?.Regions == null) return;

            var id = 1;
            if (Scene.Regions.Count > 0)
            {
                while (Scene.Regions.Any(r => r.Id == id && !r.IsBackgound)) ++id;
            }

            var color = TargetColors[(id - 1) % TargetColors.Count];
            //Debug.WriteLine($"[Debug]: add target, width={MediaWidth} height={MediaHeight}");
            var target = new Models.Region(id, $"Target {id}", DefaultSize, MediaWidth / 2, MediaHeight / 2, false, color);
            Scene.Regions.Add(target);
            SelectedRegion = target;
            Scene.Save();
        }

        [RelayCommand]
        void AddBackground()
        {
            if (Scene?.Regions == null) return;

            var id = 1;
            if (Scene.Regions.Count > 0)
            {
                while (Scene.Regions.Any(r => r.Id == id && r.IsBackgound)) ++id;
            }

            var color = "White";
            var target = new Models.Region(id, $"Background {id}", DefaultSize, MediaWidth / 2, MediaHeight / 2, true, color);
            Scene.Regions.Add(target);
            SelectedRegion = target;
            Scene.Save();
        }

        [RelayCommand]
        void ShowRegions()
        {
            SelectedRegion = null;
        }
    }
}
