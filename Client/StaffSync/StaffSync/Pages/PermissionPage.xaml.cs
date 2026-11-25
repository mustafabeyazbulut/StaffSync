using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class PermissionPage : ContentPage
    {
        public PermissionPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<PermissionViewModel>()!)
        {
        }

        public PermissionPage(PermissionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
