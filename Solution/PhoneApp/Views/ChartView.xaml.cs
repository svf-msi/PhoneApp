using LiveChartsCore.Kernel.Sketches;
using MicroVue.ViewModels;
using System.Diagnostics;

namespace MicroVue.Views;

public partial class ChartView : ContentView
{
	public ChartView()
	{
		InitializeComponent();
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (bottomPanel != null) bottomPanel.IsVisible = width <= height;
        if (sidePanel != null) sidePanel.IsVisible = width > height;
    }

    void XamlAxis_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (sender is ICartesianAxis axis && BindingContext is AnalysisViewModel vm)
        {
            if (args.PropertyName == nameof(axis.MinLimit))
            {
                vm.MinX = axis.MinLimit ?? 0;
                //Debug.WriteLine($"[Debug]: Change {args.PropertyName} to {axis.MinLimit}");
            }
            else if (args.PropertyName == nameof(axis.MaxLimit))
            {
                //Debug.WriteLine($"[Debug]: Change {args.PropertyName} to {axis.MaxLimit}");
                vm.MaxX = axis.MaxLimit ?? 0;
            }
        }
    }
}