using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroVue.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MicroVue.ViewModels
{
    public partial class ImportViewModel : ObservableObject
    {
        string sceneName = "";
        public string SceneName { get => sceneName; set { SetProperty(ref sceneName, value); DuplicateScene = false; } }

        [ObservableProperty]
        string fileName = "";

        [ObservableProperty]
        string filePath = "";

        [ObservableProperty]
        bool fileSelected;

        [ObservableProperty]
        bool duplicateScene;

        [RelayCommand]
        public async Task PickFile()
        {
            try
            {
                PickOptions options = new()
                {
                    PickerTitle = "Select a video file",
                    FileTypes = FilePickerFileType.Videos,
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    FilePath = result.FullPath;
                    FileName = result.FileName;
                    FileSelected = !string.IsNullOrEmpty(FileName);
                    SceneName = Path.GetFileNameWithoutExtension(FilePath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in file picker: {e}");
            }
        }

        [RelayCommand]
        public async Task ImportScene()
        {
            try
            {
                if (!FileSelected || !File.Exists(FilePath)) return;

                if (Scene.Create(FilePath, SceneName, out var scene, out var duplicate))
                {
                    FileName = FilePath = "";
                    FileSelected = false;
                    _ = GoBack();
                }
                else
                {
                    DuplicateScene = duplicate;
                }
                
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in saving scene: {e}");
            }
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("//ScenesPage");
        }
    }
}
