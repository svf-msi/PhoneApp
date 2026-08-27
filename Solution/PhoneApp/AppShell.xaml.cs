using MicroVue.ViewModels;

namespace MicroVue
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
