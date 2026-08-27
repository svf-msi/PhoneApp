namespace MicroVue.Views;

public partial class AboutPage : ContentPage
{
	public AboutPage()
	{
		InitializeComponent();
        version_label.Text = $"MicroVue™ version {AppInfo.Current.VersionString}";
    }
}