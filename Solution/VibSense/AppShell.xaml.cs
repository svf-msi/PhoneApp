using VibSense.ViewModels;

namespace VibSense
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            BindingContext = AppShellViewModel.Instance;
        }
    }
}
