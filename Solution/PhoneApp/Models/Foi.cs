using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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

        [ObservableProperty]
        bool isNotProcessed = true;

        [ObservableProperty]
        bool isProcessing = false;

        [ObservableProperty]
        bool isReady = false;

        [JsonIgnore]
        [ObservableProperty]
        double progress;

        [JsonIgnore]
        public CancellationTokenSource Cts { get; set; }
    }
}
