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
            string[] folders = Directory.GetDirectories(App.DataFolder);
            foreach (string folder in folders)
            {
                //Debug.WriteLine($"[Debug]: {folder}");
                var fullName = Path.GetFileName(folder);
                var sceneName = Path.GetFileNameWithoutExtension(folder);
                var creationTime = File.GetCreationTime(folder);
                var isSceneFolder = fullName.Contains(SceneItem.DefaultExtension);
                var scene = new SceneItem { Name = sceneName, Date = creationTime, ItemPath = folder, Type = isSceneFolder ? ItemType.SceneFolder : ItemType.Folder };
                sceneItems.Add(scene);
            }
            Scenes = sceneItems;
        }

        //public void Refresh()
        //{
        //    var sceneItems = new ObservableCollection<SceneItem>();
        //    string[] files = Directory.GetFiles(App.DataFolder);
        //    foreach (string file in files)
        //    {
        //        var name = Path.GetFileName(file);
        //        var creationTime = File.GetCreationTime(file);
        //        var scene = new SceneItem { Name = name, Date = creationTime, ItemPath = file };
        //        sceneItems.Add(scene);
        //    }
        //    Scenes = sceneItems;
        //}

        [RelayCommand]
        void Delete(SceneItem sceneItem)
        {
            if (sceneItem != null)
            {
                try
                {
                    var scenePath = sceneItem.ItemPath;
                    if (!Directory.Exists(scenePath) || sceneItem.Type != ItemType.SceneFolder) return;
                    Directory.Delete(scenePath, true);
                    Scenes.Remove(sceneItem);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error in deleting scene: {e}");
                }
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
