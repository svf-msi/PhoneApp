using System.Diagnostics;
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

    private async void Button_Clicked(object sender, EventArgs args)
    {
        try
        {
            if (BindingContext is ProjectsViewModel vm)
            {
                var count = vm.Projects?.Count ?? 0;
                string result = await DisplayPromptAsync(
                    title: "Add New Project",
                    message: "(select project name)",
                    accept: "OK",
                    cancel: "Cancel",
                    maxLength: 50,
                    keyboard: Keyboard.Default,
                    initialValue: $"Project {count + 1}"
                );

                if (result != null)
                {
                    vm.AddProject(result);
                }
                else
                {
                    // do nothing
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"[Debug]: Error in adding project: {e}");
        }
    }
}