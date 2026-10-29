using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibSense.Models
{
    public partial class ProjectItem : ObservableObject
    {
        public static string DefaultExtension = ".project";

        [ObservableProperty]
        string name;

        [ObservableProperty]
        DateTime date;

        [ObservableProperty]
        string itemPath;

        [ObservableProperty]
        ItemType type;
    }

    public enum ItemType { Folder, File }
}
