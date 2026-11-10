using VibSense.ViewModels;

namespace VibSense.Views;

public partial class ChartsPage : ContentPage
{
	public ChartsPage(ChartsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}