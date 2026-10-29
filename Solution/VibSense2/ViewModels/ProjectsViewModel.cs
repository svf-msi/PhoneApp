using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibSense.Models;
using VibSense.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace VibSense.ViewModels
{
    public partial class ProjectsViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ProjectItem> projects = new ObservableCollection<ProjectItem>();

        public ProjectsViewModel()
        {
            
        }

        public void Refresh()
        {
            var sceneItems = new ObservableCollection<ProjectItem>();
            string[] folders = Directory.GetDirectories(App.DataFolder);
            foreach (string folder in folders)
            {
                //Debug.WriteLine($"[Debug]: {folder}");
                var fullName = Path.GetFileName(folder);
                var sceneName = Path.GetFileNameWithoutExtension(folder);
                var creationTime = File.GetCreationTime(folder);
                var isProject = fullName.Contains(ProjectItem.DefaultExtension);
                var scene = new ProjectItem { Name = sceneName, Date = creationTime, ItemPath = folder, Type = isProject ? ItemType.File : ItemType.Folder };
                sceneItems.Add(scene);
            }
            Projects = sceneItems;
        }

        [RelayCommand]
        void Delete(ProjectItem sceneItem)
        {
            if (sceneItem != null)
            {
                try
                {
                    var scenePath = sceneItem.ItemPath;
                    if (!Directory.Exists(scenePath) || sceneItem.Type != ItemType.File) return;
                    Directory.Delete(scenePath, true);
                    Projects.Remove(sceneItem);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error in deleting projet: {e}");
                }
            }
        }

        [RelayCommand]
        async Task Tap(ProjectItem projectItem)
        {
            //if (projectItem != null)
            //{
            //    await Shell.Current.GoToAsync(nameof(ProjectPage), 
            //        new Dictionary<string, object>
            //        {
            //            {"SceneItem", projectItem}
            //        });
            //}
        }
    }
}
