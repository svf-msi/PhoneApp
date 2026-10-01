using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VibSense.Models;

namespace VibSense.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        string label = "";

        [ObservableProperty]
        int count = 0;

        DateTime start;

        public List<Vector3> Data { get; set; } = new List<Vector3>();

        AccelerometerService accelerometerService;

        public MainViewModel()
        {
            accelerometerService = new AccelerometerService();
            accelerometerService.AccelerometerChanged += AccelerometerChanged;
        }

        public void Start()
        {
            Data.Clear();
            accelerometerService.Start();
            start = default;
        }

        public void Stop()
        {
            accelerometerService.Stop();
        }

        void AccelerometerChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var acceleration = e.Reading.Acceleration;
            //System.Diagnostics.Debug.WriteLine($"X: {data.Acceleration.X}, Y: {data.Acceleration.Y}, Z: {data.Acceleration.Z}");
            Data.Add(acceleration);
            Count = Data.Count;
            var stop = DateTime.Now;
            if (start == default) start = stop;
            var secs = (stop - start).TotalSeconds;
            if (secs > 0)
            Label = $"Rate = {(Count - 1) / secs} Hz";
        }
    }
}
