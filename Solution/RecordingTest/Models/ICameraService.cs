using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingTest.Models
{
    public interface ICameraService : INotifyPropertyChanged
    {
        #region State

        bool IsRecording { get; }
        CameraCapabilities Capabilities { get; }

        #endregion

        #region Live parameter values

        double FrameRate { get; set; }
        double Exposure { get; set; }
        double Gain { get; set; }
        bool AutoExposure { get; set; }
        double RecordingDuration { get; set; }

        #endregion

        #region Camera state

        Task<bool> Open(CameraFacing facing = CameraFacing.Back);
        void Close();

        void StartRecording();
        void StopRecording(bool discard);
        Task SwitchFacing();

        #endregion

    }
}
