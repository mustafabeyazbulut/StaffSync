using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<LoginViewModel>()!)
        {
        }

        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
