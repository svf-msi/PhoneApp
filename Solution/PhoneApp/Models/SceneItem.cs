using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroVue.Models
{
    public partial class SceneItem : ObservableObject
    {
        [ObservableProperty]
        string name;

        [ObservableProperty]
        DateTime date;

        [ObservableProperty]
        string itemPath;

        [ObservableProperty]
        ItemType type;
    }

    public enum ItemType { File, Folder }
}
