using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace MicroVue.Views;

public partial class MainView : ContentView
{
    bool isDragging = false;

    public MainView()
	{
		InitializeComponent();
        //slider.Value = player.Position.TotalSeconds;
        //slider.Maximum = player.Duration.TotalSeconds;
        //slider.Player = player;
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
}