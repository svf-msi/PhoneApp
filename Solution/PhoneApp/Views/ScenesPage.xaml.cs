using MicroVue.ViewModels;

namespace MicroVue.Views;

public partial class ScenesPage : ContentPage
{
	public ScenesPage(ScenesViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ScenesViewModel)?.Refresh();
    }
}