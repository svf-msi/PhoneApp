using VibSense.ViewModels;

namespace VibSense.Views;

public partial class ProjectsPage : ContentPage
{
	public ProjectsPage(ProjectsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ProjectsViewModel)?.Refresh();
    }
}