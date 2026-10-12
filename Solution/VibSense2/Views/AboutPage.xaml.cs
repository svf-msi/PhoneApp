using MicroVue.ViewModels;

namespace MicroVue.Views;

public partial class AboutPage : ContentPage
{
	public AboutPage(AboutViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}