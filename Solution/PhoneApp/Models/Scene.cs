using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using StandardLib;

namespace MicroVue.Models
{
    public partial class Scene : ObservableObject
    {
        #region Static section
        public static Scene Read(string file)
        {
            if (!File.Exists(file)) return null;

            var text = File.ReadAllText(file);
            if (string.IsNullOrEmpty(text)) return null;

            try
            {
                var settings = new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                };
                var scene = JsonConvert.DeserializeObject<Scene>(text, settings);
                scene.FileName = file;
                return scene;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in reading scene: {e}");
                return null;
            }
        }

        #endregion

        #region Fields and Properties

        [ObservableProperty]
        string fileName = "";

        [ObservableProperty]
        string name = "None";

        [ObservableProperty]
        string videoName = "";

        [ObservableProperty]
        double timeInterval = 1;

        [ObservableProperty]
        ObservableCollection<Region> regions = new ObservableCollection<Region>();

        [ObservableProperty]
        ObservableCollection<Target> targets = new ObservableCollection<Target>();

        [ObservableProperty]
        ObservableCollection<Foi> fois = new ObservableCollection<Foi>();

        [JsonIgnore]
        public bool IsSaving { get; private set; }

        #endregion

        public void RefreshRegions()
        {
            //Debug.WriteLine($"[Debug]: --- Reresh regions");
            for (int i = 0; i < Regions.Count; i++)
            {
                var region = Regions[0];
                Regions.RemoveAt(0);
                Regions.Add(region);
            }
        }

        public void RefreshTargets(int currentFrame)
        {
            if (Targets?.Count > 0)
            {
                foreach (var target in Targets)
                {
                    if (target != null)
                    {
                        target.CurrentFrame = currentFrame;
                    }
                }
            }
        }

        public void Save(string file = null)
        {
            if (file == null) file = FileName;
            if (file == null || IsSaving) return;

            try
            {
                IsSaving = true;
                var text = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(file, text);
                FileInfo fileInfo = new FileInfo(file);
                
                //Debug.WriteLine($"[Debug]: {text}");
                Debug.WriteLine($"[Debug]: saved {fileInfo.Length / 1024} kbytes to {Name}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in saving scene: {e}");
            }
            finally { IsSaving = false; }
        }
    }
}
