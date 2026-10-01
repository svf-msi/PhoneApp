
namespace VibSense.Models
{
    public class AccelerometerService
    {
        public SensorSpeed Speed { get; set; } = SensorSpeed.Fastest;

        public event EventHandler<AccelerometerChangedEventArgs> AccelerometerChanged;

        public void Start()
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += AccelerometerChanged;
                    Accelerometer.Default.Start(Speed);
                }
            }
        }

        public void Stop()
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= AccelerometerChanged;
                }
            }
        }

        public void Toggle()
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += AccelerometerChanged;
                    Accelerometer.Default.Start(Speed);
                }
                else
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= AccelerometerChanged;
                }
            }
        }
    }
}
