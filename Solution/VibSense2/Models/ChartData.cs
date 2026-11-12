using Android.Bluetooth;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using StandardLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VibSense.ViewModels;

namespace VibSense.Models
{
    public class ChartData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public static ChartData Create(string name, List<Vector3> waveforms, double rate, Quaternion quaternion)
        {
            if (waveforms == null || waveforms.Count == 0) return null;

            return new ChartData()
            {
                Name = name,
                SampleRate = rate,
                Data = waveforms,
                Quaternion = quaternion
            };
        }

        public static ChartData Create2(string name, List<Vector3> waveforms, double rate)
        {
            if (waveforms == null || waveforms.Count == 0) return null;

            var waveformX = waveforms.Select(p => (double)p.X).ToArray();
            var spectrumX = FftAnalysis.Emgu(waveformX, WindowType.Hann);
            var waveformY = waveforms.Select(p => (double)p.Y).ToArray();
            var spectrumY = FftAnalysis.Emgu(waveformY, WindowType.Hann);
            var waveformZ = waveforms.Select(p => (double)p.Z).ToArray();
            var spectrumZ = FftAnalysis.Emgu(waveformZ, WindowType.Hann);

            var span = spectrumX.GetLength(1);
            var values = new List<Vector3>();
            values.Add(new Vector3((float)spectrumX[0, 0] / 2, (float)spectrumY[0, 0] / 2, (float)spectrumZ[0, 0] / 2));
            for (int i = 1; i < span / 2; ++i)
            {
                var dispX = (float)FftAnalysis.Complex(spectrumX, 0, 2 * i - 1).Magnitude;
                var dispY = (float)FftAnalysis.Complex(spectrumY, 0, 2 * i - 1).Magnitude;
                var dispZ = (float)FftAnalysis.Complex(spectrumZ, 0, 2 * i - 1).Magnitude;
                values.Add(new Vector3(dispX, dispY, dispZ));
            }

            return new ChartData()
            {
                Name = name,
                SampleRate = rate,
                Data = values,
            };
        }

        public static List<Vector3> Fft(List<Vector3> waveforms)
        {
            if (waveforms == null || waveforms.Count == 0) return null;

            var waveformX = waveforms.Select(p => (double)p.X).ToArray();
            var spectrumX = FftAnalysis.Emgu(waveformX, WindowType.Hann);
            var waveformY = waveforms.Select(p => (double)p.Y).ToArray();
            var spectrumY = FftAnalysis.Emgu(waveformY, WindowType.Hann);
            var waveformZ = waveforms.Select(p => (double)p.Z).ToArray();
            var spectrumZ = FftAnalysis.Emgu(waveformZ, WindowType.Hann);

            var span = spectrumX.GetLength(1);
            var values = new List<Vector3>();
            values.Add(new Vector3((float)spectrumX[0, 0] / 2, (float)spectrumY[0, 0] / 2, (float)spectrumZ[0, 0] / 2));
            for (int i = 1; i < span / 2; ++i)
            {
                var dispX = (float)FftAnalysis.Complex(spectrumX, 0, 2 * i - 1).Magnitude;
                var dispY = (float)FftAnalysis.Complex(spectrumY, 0, 2 * i - 1).Magnitude;
                var dispZ = (float)FftAnalysis.Complex(spectrumZ, 0, 2 * i - 1).Magnitude;
                values.Add(new Vector3(dispX, dispY, dispZ));
            }

            return values;
        }

        public string Name { get; set; }

        public Int64 TimeStamp { get; set; }

        public double SampleRate { get; set; }

        public Quaternion Quaternion { get; set; }

        public List<Vector3> Data { get; set; }

        bool selected = true;
        [JsonIgnore]
        public bool Selected { get => selected; set { selected = value; NotifyPropertyChanged(); } }

        [JsonIgnore]
        public List<Vector3> FftInternal { get; set; }

        [JsonIgnore]
        public List<Vector3> FftGlobal { get; set; }

        [JsonIgnore]
        public bool IsValid { get; set; }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Initialize()
        {
            try
            {
                IsValid = false;

                // Internal coordinates
                if (Data == null || Data.Count == 0) return;
                FftInternal = Fft(Data);

                // Global coordinates
                if (Data == null || Data.Count == 0) return;
                var rotatedData = Data.Select(v => Vector3.Transform(v, Quaternion)).ToList();
                FftGlobal = Fft(rotatedData);

                IsValid = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in data initializing: {e}");
            }
        }

        public List<Vector3> GetFft(Coordinates coordinates)
        {
            if (coordinates == Coordinates.Internal)
            {
                return FftInternal;
            }
            else
            { 
                return FftGlobal; 
            }
        }
    }
}
