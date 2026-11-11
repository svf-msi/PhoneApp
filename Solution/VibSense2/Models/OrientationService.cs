using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VibSense.Models
{
    public class OrientationService
    {
        public EventHandler OnOrientationChanged;

        public double Pitch { get; protected set; } // degrees

        public double Yaw { get; protected set; }

        public double Roll { get; protected set; }

        public Quaternion Quaternion { get; protected set; }

        public void Start()
        {
            if (OrientationSensor.Default.IsSupported)
            {
                OrientationSensor.Default.ReadingChanged += OnReadingChanged;
                OrientationSensor.Default.Start(SensorSpeed.UI);
                Debug.WriteLine($"[Debug]: Orientation service started.");
            }
        }

        public void Stop()
        {
            if (OrientationSensor.Default.IsSupported)
            {
                OrientationSensor.Default.Stop();
                OrientationSensor.Default.ReadingChanged -= OnReadingChanged;
                Debug.WriteLine($"[Debug]: Orientation service stopped.");
            }
        }

        void OnReadingChanged(object sender, OrientationSensorChangedEventArgs args)
        {
            //Debug.WriteLine($"[Debug]: On reading changed {args.Reading}.");
            var data = args.Reading;
            Quaternion = data.Orientation;

            (Roll, Pitch, Yaw) = QuaternionToEulerAngles(
                data.Orientation.W,
                data.Orientation.X,
                data.Orientation.Y,
                data.Orientation.Z
            );
            //Debug.WriteLine($"[Debug]: On reading changed {Roll}, {Pitch}, {Yaw}.");
            OnOrientationChanged?.Invoke(this, args);
        }

        public static (double Roll, double Pitch, double Yaw) QuaternionToEulerAngles(float w, float x, float y, float z)
        {
            // Roll (x-axis rotation)
            double sinr_cosp = 2.0 * (w * x + y * z);
            double cosr_cosp = 1.0 - 2.0 * (x * x + y * y);
            double roll = Math.Atan2(sinr_cosp, cosr_cosp);

            // Pitch (y-axis rotation)
            double sinp = 2.0 * (w * y - z * x);
            double pitch;
            if (Math.Abs(sinp) >= 1)
                pitch = Math.CopySign(Math.PI / 2, sinp); // use 90 degrees if out of range
            else
                pitch = Math.Asin(sinp);

            // Yaw (z-axis rotation)
            double siny_cosp = 2.0 * (w * z + x * y);
            double cosy_cosp = 1.0 - 2.0 * (y * y + z * z);
            double yaw = Math.Atan2(siny_cosp, cosy_cosp);

            return 
            (
                Roll: roll * (180.0 / Math.PI),
                Pitch: pitch * (180.0 / Math.PI),
                Yaw: yaw * (180.0 / Math.PI)
            );
        }

        ~OrientationService() { Stop(); }
    }
}
