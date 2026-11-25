using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ContactDetailPage : ContentPage
    {
        public ContactDetailPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<ContactDetailViewModel>()!)
        {
        }

        public ContactDetailPage(ContactDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
