using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroVue.Models;
using MicroVue.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace MicroVue.ViewModels
{
    public partial class ScenesViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<SceneItem> scenes = new ObservableCollection<SceneItem>();

        public ScenesViewModel()
        {
            
        }

        public void Refresh()
        {
            var sceneItems = new ObservableCollection<SceneItem>();
            string[] files = Directory.GetFiles(App.DataFolder);
            foreach (string file in files)
            {
                var name = Path.GetFileName(file);
                var creationTime = File.GetCreationTime(file);
                var scene = new SceneItem { Name = name, Date = creationTime, ItemPath = file };
                sceneItems.Add(scene);
            }
            Scenes = sceneItems;
        }

        [RelayCommand]
        void Delete(SceneItem sceneItem)
        {
            if (sceneItem != null)
            {
                var scenePath = sceneItem.ItemPath;
                if (!File.Exists(scenePath)) return;
                var text = File.ReadAllText(scenePath);
                if (string.IsNullOrEmpty(text)) return;

                var scene = JsonConvert.DeserializeObject<Scene>(text);
                var videoPath = scene?.VideoName;
                if (!string.IsNullOrEmpty(scenePath)) File.Delete(scenePath);
                if (!string.IsNullOrEmpty(videoPath)) File.Delete(videoPath);
                Scenes.Remove(sceneItem);
            }
        }

        [RelayCommand]
        async Task Tap(SceneItem sceneItem)
        {
            if (sceneItem != null)
            {
                await Shell.Current.GoToAsync(nameof(AnalysisPage), 
                    new Dictionary<string, object>
                    {
                        {"SceneItem", sceneItem}
                    });
            }
        }
    }
}
