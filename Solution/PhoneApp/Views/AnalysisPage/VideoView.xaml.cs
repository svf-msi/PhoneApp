namespace MicroVue.Views;

public partial class VideoView : ContentView
{
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
}