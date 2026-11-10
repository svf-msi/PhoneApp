using Android.Bluetooth;
using Android.Icu.Number;
using LiveChartsCore.Defaults;
using StandardLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VibSense.Models
{
    public class DataChart
    {
        public static DataChart Create(string name, List<Vector3> waveforms, double rate)
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
            values.Add(new Vector3(0, 0, 0));
            for (int i = 1; i < span / 2; ++i)
            {
                var dispX = (float)FftAnalysis.Complex(spectrumX, 0, 2 * i - 1).Magnitude;
                var dispY = (float)FftAnalysis.Complex(spectrumY, 0, 2 * i - 1).Magnitude;
                var dispZ = (float)FftAnalysis.Complex(spectrumZ, 0, 2 * i - 1).Magnitude;
                values.Add(new Vector3(dispX, dispY, dispZ));
            }

            return new DataChart()
            {
                Name = name,
                SampleRate = rate,
                Data = values,
            };
        }

        public string Name { get; set; }

        public Int64 TimeStamp { get; set; }

        public double SampleRate { get; set; }

        public List<Vector3> Data { get; set; }
    }
}
