using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroVue.Models;
using MicroVue.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MicroVue.ViewModels
{
    public partial class ScenesViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<SceneItem> scenes;

        public ScenesViewModel()
        {
            Scenes = new ObservableCollection<SceneItem>()
            {
                new SceneItem { Name = "Scene 1", Date = new DateTime(2025, 10, 10) },
                new SceneItem { Name = "Scene 2", Date = new DateTime(2026, 2, 15) },
                new SceneItem { Name = "Scene 3", Date = new DateTime(2026, 3, 20) },
            };
        }

        [RelayCommand]
        void Delete(SceneItem scene)
        {
            if (scene != null) Scenes.Remove(scene);
        }

        [RelayCommand]
        async Task Tap(SceneItem scene)
        {
            if (scene != null)
            {
                await Shell.Current.GoToAsync(nameof(AnalysisPage), 
                    new Dictionary<string, object>
                    {
                        {"SceneItem", scene}
                    });
            }
        }
    }
}
