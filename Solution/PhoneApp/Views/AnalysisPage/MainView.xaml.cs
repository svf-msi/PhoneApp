using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using MicroVue.ViewModels;
using System.Diagnostics;

namespace MicroVue.Views;

public partial class MainView : ContentView
{
    bool isDragging = false;

    public MainView()
	{
		InitializeComponent();
        player.Speed = 1;
    }

    void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (sender is AnalysisViewModel vm)
        {
            if (args.PropertyName == nameof(vm.CurrentTime))
            {
                //Debug.WriteLine($"[Debug]: current time = {vm.CurrentTime} => {vm.GetCurrentTime()}");
                player.SeekTo(TimeSpan.FromSeconds(vm.GetCurrentTime()), CancellationToken.None);
            }
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (bottomPanel != null) bottomPanel.IsVisible = width <= height;
        if (sidePanel != null) sidePanel.IsVisible = width > height;
        SetupOverlay();
    }

    void SetupOverlay()
    {
        if (player == null) return;
        var w = player.MediaWidth;
        var h = player.MediaHeight;
        //Debug.WriteLine($"[Debug]: player params {player.MediaWidth}, {player.MediaHeight}");
        if (w == 0 || h == 0) return;
        var aspect = (double)w / h;
        var rw = player.Height * aspect;
        var rh = player.Width / aspect;
        rw = Math.Min(player.Width, rw);
        rh = Math.Min(player.Height, rh);
        var off_x = player.Width - rw;
        var off_y = player.Height - rh;
        var bounds = new Rect(off_x / 2, off_y / 2, rw, rh);
        AbsoluteLayout.SetLayoutBounds(overlay, bounds);
        AbsoluteLayout.SetLayoutBounds(overlay2, bounds);
        if (BindingContext is AnalysisViewModel vm)
        {
            vm.MediaWidth = player.MediaWidth;
            vm.MediaHeight = player.MediaHeight;
            vm.PlayerWidth = rw;
            vm.PlayerHeight = rh;
            vm.PlayerScale = rw / w;
            vm.Scene?.RefreshRegions();
            vm.Scene?.RefreshTargets(0);
            //Debug.WriteLine($"[Debug]: media = {vm.MediaWidth}, {vm.MediaHeight}, {vm.MediaRotation}");
        }
    }

    void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (player != null)
        {
            if (args.PropertyName == nameof(player.MediaWidth) || args.PropertyName == nameof(player.MediaHeight))
            {
                SetupOverlay();
            }
            else if (args.PropertyName == nameof(player.Duration))
            {
                slider.Maximum = player.Duration.TotalSeconds;
                if (BindingContext is AnalysisViewModel vm)
                {
                    vm.SetDuration(slider.Maximum);
                    if (vm.EndTime == 0)
                    {
                        vm.EndTime = vm.Duration;
                    }
                    vm.Refresh();
                    var position = vm.GetStartPosition();
                    player.SeekTo(TimeSpan.FromSeconds(position), CancellationToken.None);
                    vm.PropertyChanged += ViewModel_PropertyChanged;

                    //Debug.WriteLine($"[Debug]: Player duration {player.Duration.TotalSeconds}, {vm.Duration}, {vm.EndTime}");
                }
            }
        }
    }

    void Player_PositionChanged(object sender, MediaPositionChangedEventArgs args)
    {
        //Debug.WriteLine($"[Debug]: Player position changed: {args.Position.TotalSeconds}");
        if (!isDragging)
        {
            slider.Value = args.Position.TotalSeconds;
            if (BindingContext is AnalysisViewModel vm)
            {
                vm.Scene?.RefreshTargets((int)Math.Round(slider.Value / slider.Maximum * vm.MediaLength));
                vm.SetCurrentTime(slider.Value);
            }
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
            if (BindingContext is AnalysisViewModel vm)
            {
                vm.Scene?.RefreshTargets((int)Math.Round(slider.Value / slider.Maximum * vm.MediaLength));
                vm.SetCurrentTime(slider.Value);
            }
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