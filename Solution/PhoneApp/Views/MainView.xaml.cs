using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace MicroVue.Views;

public partial class MainView : ContentView
{
    bool isDragging = false;

    public MainView()
	{
		InitializeComponent();
        player.Speed = 1;
    }

    void OnPlayPauseButtonClicked(object sender, EventArgs args)
    {
        if (player.CurrentState == MediaElementState.Stopped ||
            player.CurrentState == MediaElementState.Paused)
        {
            player.Play();
        }
        else if (player.CurrentState == MediaElementState.Playing)
        {
            player.Pause();
        }
    }

    void OnMediaPositionChanged(object sender, MediaPositionChangedEventArgs args)
    {
        if (!isDragging)
        {
            slider.Maximum = player.Duration.TotalSeconds;
            slider.Value = args.Position.TotalSeconds;
        }
    }

    void OnSliderDragStarted(object sender, EventArgs e)
    {
        isDragging = true;
    }

    async void OnSliderDragCompleted(object sender, EventArgs e)
    {
        var targetPosition = TimeSpan.FromSeconds(slider.Value);

        await player.SeekTo(targetPosition, CancellationToken.None);
        isDragging = false;
    }

    void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        switch (picker.SelectedIndex)
        {
            case 1:
                player.Speed = 2;
                break;
            case 2:
                player.Speed = 10;
                break;
            default:
                player.Speed = 1;
                break;
        }
    }

    protected void Picker_HandlerChanged(object sender, EventArgs e)
    {
        base.OnHandlerChanged();

#if ANDROID
        if (picker.Handler?.PlatformView is AndroidX.AppCompat.Widget.AppCompatEditText nativePicker)
        {
            // Removes the baseline background completely
            nativePicker.Background = null;
        }
#endif
    }
}