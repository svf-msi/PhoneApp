using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using MicroVue.ViewModels;
using Newtonsoft.Json;
using System.Diagnostics;

namespace MicroVue.Views;

public partial class MainPanel : ContentView
{
    public MainPanel()
	{
		InitializeComponent();
	}

    void Info_Pressed(object sender, EventArgs e)
    {
        if (BindingContext is AnalysisViewModel vm)
        {
            var infoView = new InfoView();
            infoView.BindingContext = vm;

            Task.Run(async () =>
            {
                var scale = vm.Scene.CalibrationScale;
                var units = vm.Scene.DistanceUnits;
                await Shell.Current.ShowPopupAsync(infoView);
                if (scale != vm.Scene.CalibrationScale || units != vm.Scene.DistanceUnits)
                {
                    vm.UpdateChart();
                    vm.Scene.Save();
                }
                //Debug.WriteLine($"[Debug]: {JsonConvert.SerializeObject(vm.Scene.Calibration)}");

            });
        }
    }
}