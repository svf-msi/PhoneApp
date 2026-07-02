using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroVue.Models;
using Newtonsoft.Json;
using SkiaSharp;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MicroVue.ViewModels
{
    [QueryProperty(nameof(SceneItem), "SceneItem")]
    public partial class AnalysisViewModel : ObservableObject
    {
        [ObservableProperty]
        SceneItem sceneItem = new SceneItem();

        [ObservableProperty]
        Scene scene;

        [ObservableProperty]
        string sceneName;

        [ObservableProperty]
        string videoPath;


        [ObservableProperty]
        bool onMainView = true;

        [ObservableProperty]
        bool onChartView;

        [ObservableProperty]
        bool onVideoView;

        [ObservableProperty]
        ImageSource image;

        [ObservableProperty]
        int rotation;

        [ObservableProperty]
        int currentFrame = 0;

        [ObservableProperty]
        bool isPlaying;

        [ObservableProperty]
        bool back;

        Video_MP4 video;

        partial void OnSceneItemChanged(SceneItem sceneItem)
        {
            if (sceneItem != null)
            {
                SceneName = sceneItem.Name;
                var scenePath = sceneItem.ItemPath;
                if (!File.Exists(scenePath)) return;
                var text = File.ReadAllText(scenePath);
                if (string.IsNullOrEmpty(text)) return;

                Scene = JsonConvert.DeserializeObject<Scene>(text);
                VideoPath = Scene?.VideoName;
                if (!string.IsNullOrEmpty(VideoPath))
                {
                    video = new Video_MP4(VideoPath);
                    CurrentFrame = 0;
                    SetImage();
                }
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
                Rotation = 90;
            }
        }

        async Task StartPlay()
        {
            if (IsPlaying || video == null) return;
            try
            {
                IsPlaying = true;
                var length = video.Length;
                while (IsPlaying)
                {
                    ++CurrentFrame;
                    if (CurrentFrame >= length) CurrentFrame = 0;
                    SetImage();
                    await Task.Delay(100);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in video play: {e}");
            }
        }

        partial void OnBackChanged(bool value)
        {
            if (value) _ = GoBack();
        }

        [RelayCommand]
        async Task PlayPause()
        {
            if (IsPlaying) IsPlaying = false;
            else _ = StartPlay();
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
