using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Models
{
    public partial class Foi : ObservableObject
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        string name = "FOI";

        [ObservableProperty]
        double frequency;

        [ObservableProperty]
        double binSize;
    }
}
