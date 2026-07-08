using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using MicroVue.ViewModels;
using System.Diagnostics;

namespace MicroVue.Views;

public partial class MainPanel : ContentView
{
    public MediaElement Player { get; set; }

    double duration;
    public double Duration { get => duration; set { duration = value; slider.Maximum = Duration; } }

    double position;
    public double Position { get => position; set { position = value; if (!isDragging) slider.Value = Position; } }

    bool isDragging = false;

    public MainPanel()
	{
		InitializeComponent();
	}

    void OnPlayPauseButtonClicked(object sender, EventArgs args)
    {
        //Debug.WriteLine($"Clicked: player {Player.CurrentState}");
        if (Player.CurrentState == MediaElementState.Stopped ||
            Player.CurrentState == MediaElementState.Paused)
        {
            Player.Play();
            if (BindingContext is AnalysisViewModel vm)
            {
                vm.IsPlaying = true;
            }
        }
        else if (Player.CurrentState == MediaElementState.Playing)
        {
            Player.Pause();
            if (BindingContext is AnalysisViewModel vm)
            {
                vm.IsPlaying = false;
            }
        }
    }

    void OnSliderDragStarted(object sender, EventArgs e)
    {
        isDragging = true;
    }

    async void OnSliderDragCompleted(object sender, EventArgs e)
    {
        //Debug.WriteLine($"[Debug]: Drag completed");
        if (sender is Slider s)
        {
            var targetPosition = TimeSpan.FromSeconds(s.Value);

            await Player.SeekTo(targetPosition, CancellationToken.None);
            isDragging = false;
            //Debug.WriteLine($"[Debug]: Drag completed: {s.Value}, {s.Maximum}, {targetPosition}, {Player.Position}");
        }
    }

    void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker)
        {
            switch (picker.SelectedIndex)
            {
                case 1:
                    Player.Speed = 2;
                    break;
                case 2:
                    Player.Speed = 10;
                    break;
                default:
                    Player.Speed = 1;
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