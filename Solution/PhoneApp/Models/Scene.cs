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

        public void Save(string file = null)
        {
            if (file == null) file = FileName;
            if (file == null) return;
            var text = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(file, text);
        }
    }
}
