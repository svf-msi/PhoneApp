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

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0) return;

        TargetOverlay.WidthRequest = TargetOverlay.HeightRequest = Math.Round(Math.Min(width, height) * 0.45);

        bool landscape = width > height;
        CountdownCard.HorizontalOptions = landscape ? LayoutOptions.Start : LayoutOptions.Center;
        CountdownCard.VerticalOptions = landscape ? LayoutOptions.Center : LayoutOptions.Start;
    }

    private void OnDialogTapped(object sender, TappedEventArgs e)
    {
        DurationEntry.Unfocus();
        VideoNameEntry.Unfocus();
    }

}