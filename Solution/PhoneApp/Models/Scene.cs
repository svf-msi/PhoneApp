using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ObservableCollection<Region> regions = new ObservableCollection<Region>() { new Region { Rect = new Rect(0.1, 0.1, 0.2, 0.3) } };
    }
}
