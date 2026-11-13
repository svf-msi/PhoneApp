using VibSense.ViewModels;

#if ANDROID
using Android.Content.PM;
#endif

namespace VibSense.Views;

public partial class LivePage : ContentPage
{
    public LivePage(LiveViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LiveViewModel vm)
        {
            vm.StartOrientationService();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is LiveViewModel vm)
        {
            vm.StopOrientationService();
        }
    }

    void Button_Pressed(object sender, EventArgs e)
    {
        if (BindingContext is LiveViewModel vm)
        {
            vm.Start();
        }
    }

    void Button_Released(object sender, EventArgs e)
    {
        if (BindingContext is LiveViewModel vm)
        {

            vm.Stop();
        }
    }
}