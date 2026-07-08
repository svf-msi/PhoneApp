using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Models
{
    public partial class Region : ObservableObject
    {
        [ObservableProperty]
        string name = "Region";

        [ObservableProperty]
        Rect rect;

        [ObservableProperty]
        private Color color = Colors.Red;
    }
}
