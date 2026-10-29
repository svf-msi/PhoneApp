using VibSense.ViewModels;

namespace VibSense.Views;

public partial class AboutPage : ContentPage
{
	public AboutPage(AboutViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}