using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using MicroVue.ViewModels;
using System.Diagnostics;

namespace MicroVue.Views;

public partial class MainPanel : ContentView
{
    public MainPanel()
	{
		InitializeComponent();
	}

    void Info_Pressed(object sender, EventArgs e)
    {
        if (BindingContext is AnalysisViewModel vm)
        {
            var infoView = new InfoView();
            infoView.BindingContext = vm;

            Shell.Current.ShowPopupAsync(infoView); 
        }
    }
}