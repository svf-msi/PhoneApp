using RecordingTest.ViewModels;

namespace RecordingTest
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Permissions.RequestAsync<Permissions.Camera>();
            BindingContext = new MainViewModel();
        }
    }
}
