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
}