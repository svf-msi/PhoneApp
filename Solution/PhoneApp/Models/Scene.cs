using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroVue.Models
{
    public partial class Scene : ObservableObject
    {
        [ObservableProperty]
        string name;

        [ObservableProperty]
        string videoName;
    }
}
