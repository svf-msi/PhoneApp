using System.Diagnostics;
using VibSense.ViewModels;

namespace VibSense.Views;

public partial class ProjectsPage : ContentPage
{
    bool firstOpen = true;

	public ProjectsPage(ProjectsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ProjectsViewModel)?.Refresh();

        if (!App.Open)
        {
            App.Open = true;
            await Shell.Current.GoToAsync("//LivePage");
        }
    }

    private async void Button_Clicked(object sender, EventArgs args)
    {
        try
        {
            if (BindingContext is ProjectsViewModel vm)
            {
                var count = vm.Projects?.Count ?? 0;
                string result = await DisplayPromptAsync(
                    title: "Add New Set of Charts",
                    message: "(select set name)",
                    accept: "OK",
                    cancel: "Cancel",
                    maxLength: 50,
                    keyboard: Keyboard.Default,
                    initialValue: $"Set {count + 1}"
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