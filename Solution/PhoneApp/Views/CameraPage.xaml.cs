using MicroVue.ViewModels;

namespace MicroVue.Views;

public partial class CameraPage : ContentPage
{
	public CameraPage(CameraViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted && BindingContext is CameraViewModel vm)
            {
                vm.Initialize();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error requesting permissions: {e}");
        }
    }

    private void OnDialogTapped(object sender, TappedEventArgs e)
    {
        DurationEntry.Unfocus();
    }

}