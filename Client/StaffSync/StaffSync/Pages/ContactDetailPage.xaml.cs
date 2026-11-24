using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ContactDetailPage : ContentPage
    {
        public ContactDetailPage(ContactDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
