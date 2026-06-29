using MicroVue.ViewModels;

namespace MicroVue.Views;

public partial class ImportPage : ContentPage
{
	public ImportPage(ImportViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}