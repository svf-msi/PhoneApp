using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroVue.ViewModels
{
    public partial class ImportViewModel : ObservableObject
    {
        [ObservableProperty]
        string sceneName = "";

        [ObservableProperty]
        string fileName = "";

        [ObservableProperty]
        string filePath = "";

        [ObservableProperty]
        bool fileSelected;

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
                Console.WriteLine($"Error in file picker: {e}");
            }
        }

        [RelayCommand]
        public async Task ImportScene()
        {

        }
    }
}
