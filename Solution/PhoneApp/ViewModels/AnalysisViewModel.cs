using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroVue.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        bool back;

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
            }
        }

        partial void OnBackChanged(bool value)
        {
            if (value) _ = GoBack();
        }

        //bool onBack;
        //public bool OnBack { get => onBack; set {  SetProperty(ref onBack, value); if (OnBack) GoBack(); }  }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
