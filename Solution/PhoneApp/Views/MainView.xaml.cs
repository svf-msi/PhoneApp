using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using System.Diagnostics;

namespace MicroVue.Views;

public partial class MainView : ContentView
{
    bool isDragging = false;

    public MainView()
	{
		InitializeComponent();
        player.Speed = 1;

        player.PropertyChanged += Player_PropertyChanged;
    }

    void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (player != null)
        {
            if (args.PropertyName == nameof(player.MediaWidth) || args.PropertyName == nameof(player.MediaHeight))
            {
                SetupOverlay();
            }
        }
    }

    void SetupOverlay()
    {
        if (player == null) return;
        var w = player.MediaWidth;
        var h = player.MediaHeight;
        //Debug.WriteLine($"****** player resolution: {player.MediaWidth}, {player.MediaHeight}");
        if (w == 0 || h == 0) return;
        var aspect = (double)w / h;
        var rw = player.Height * aspect;
        var rh = player.Width / aspect;
        //Debug.WriteLine($"****** player bounds: {player.Bounds}");
        //Debug.WriteLine($"****** player frame: {player.Frame}");
        //Debug.WriteLine($"****** player height: {player.Width} vs {rw}");
        //Debug.WriteLine($"****** player height: {player.Height} vs {rh}");
        rw = Math.Min(player.Width, rw);
        rh = Math.Min(player.Height, rh);
        var off_x = player.Width - rw;
        var off_y = player.Height - rh;
        AbsoluteLayout.SetLayoutBounds(overlay, new Rect(off_x/2, off_y/2, rw, rh));
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (bottomPanel != null) bottomPanel.IsVisible = width <= height;
        if (sidePanel != null) sidePanel.IsVisible = width > height;
        SetupOverlay();
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
            slider.Maximum = slider2.Maximum = player.Duration.TotalSeconds;
            slider.Value = slider2.Value = args.Position.TotalSeconds;
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

    void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker)
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
    }

    protected void Picker_HandlerChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker)
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
}