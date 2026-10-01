using VibSense.ViewModels;

#if ANDROID
using Android.Content.PM;
#endif

namespace VibSense.Views;

public partial class MainPage : ContentPage
{

#if ANDROID
    ScreenOrientation defaultOrientation = Platform.CurrentActivity.RequestedOrientation;
#endif

    public MainPage(MainViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        defaultOrientation = Platform.CurrentActivity.RequestedOrientation;
        Platform.CurrentActivity.RequestedOrientation = ScreenOrientation.Portrait;
#endif

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

#if ANDROID
        Platform.CurrentActivity.RequestedOrientation = defaultOrientation;
#endif

    }

    void Button_Pressed(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            vm.Start();
        }
    }

    void Button_Released(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {

            vm.Stop();
        }
    }
}