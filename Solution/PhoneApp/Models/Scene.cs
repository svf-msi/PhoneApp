using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace MicroVue.Models
{
    public partial class Scene : ObservableObject
    {
        [ObservableProperty]
        string name = "None";

        [ObservableProperty]
        string videoName = "";

        [ObservableProperty]
        double timeInterval = 1;

        [ObservableProperty]
        ObservableCollection<Region> regions = new ObservableCollection<Region>();

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
    }
}
