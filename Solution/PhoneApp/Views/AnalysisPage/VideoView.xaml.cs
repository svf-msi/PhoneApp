using CommunityToolkit.Maui.Core;
using MicroVue.ViewModels;
using System.Diagnostics;

namespace MicroVue.Views;

public partial class VideoView : ContentView
{
    bool isDragging = false;

    public VideoView()
	{
		InitializeComponent();
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (bottomPanel != null) bottomPanel.IsVisible = width <= height;
        if (sidePanel != null) sidePanel.IsVisible = width > height;
    }

    void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (player != null)
        {
            if (args.PropertyName == nameof(player.Duration))
            {
                slider.Maximum = player.Duration.TotalSeconds;
                //Debug.WriteLine($"[Debug]: Player duration {player.Duration.TotalSeconds}");
            }
        }
    }

    void Player_PositionChanged(object sender, MediaPositionChangedEventArgs args)
    {
        //Debug.WriteLine($"[Debug]: Player position changed: {args.Position.TotalSeconds}");
        if (!isDragging)
        {
            slider.Value = args.Position.TotalSeconds;
        }
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

    void OnSliderDragStarted(object sender, EventArgs e)
    {
        isDragging = true;
    }

    async void OnSliderDragCompleted(object sender, EventArgs e)
    {
        if (sender is Slider slider)
        {
            var targetPosition = TimeSpan.FromSeconds(slider.Value);

            await player.SeekTo(targetPosition, CancellationToken.None);
            isDragging = false;
        }
    }

}