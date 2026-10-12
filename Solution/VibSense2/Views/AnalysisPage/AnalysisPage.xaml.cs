using MicroVue.ViewModels;

namespace MicroVue.Views;

public partial class AnalysisPage : ContentPage
{
	public AnalysisPage(AnalysisViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        topTabs.IsVisible = width <= height;
        leftTabs.IsVisible = width > height;
    }
}