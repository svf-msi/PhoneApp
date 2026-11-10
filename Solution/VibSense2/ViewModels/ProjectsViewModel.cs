using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using VibSense.Models;
using VibSense.Views;

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
            var projectsItems = new ObservableCollection<ProjectItem>();
            string[] files = Directory.GetFiles(App.DataFolder);
            foreach (string file in files)
            {
                //Debug.WriteLine($"[Debug]: {file}");
                var fullName = Path.GetFileName(file);
                var projectName = Path.GetFileNameWithoutExtension(file);
                var creationTime = File.GetCreationTime(file);
                var isProject = fullName.Contains(Project.DefaultExtension);
                var project = new ProjectItem { Name = projectName, Date = creationTime, ItemPath = file, Type = isProject ? ItemType.File : ItemType.Folder };
                projectsItems.Add(project);
            }
            Projects = projectsItems;
        }

        public void AddProject(string name = null)
        {
            if (Projects == null) Projects = new ObservableCollection<ProjectItem>();

            if (Project.Create(name, out var project, out var duplicate))
            {
                var creationTime = File.GetCreationTime(project.FileName);
                var projectItem = new ProjectItem { Name = name, Date = creationTime, ItemPath = project.FileName, Type = ItemType.File };
                Projects.Add(projectItem);
            }
            else if (duplicate)
            {

            }
        }

        [RelayCommand]
        void Delete(ProjectItem item)
        {
            if (item != null)
            {
                try
                {
                    Debug.WriteLine($"[Debug]: {item.ItemPath}");
                    var path = item.ItemPath;
                    if (!File.Exists(path) || item.Type != ItemType.File) return;
                    File.Delete(path);
                    Projects.Remove(item);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error in deleting projet: {e}");
                }
            }
        }

        [RelayCommand]
        async Task Tap(ProjectItem item)
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
