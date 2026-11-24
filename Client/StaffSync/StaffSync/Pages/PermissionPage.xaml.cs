using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class PermissionPage : ContentPage
    {
        public PermissionPage(PermissionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
