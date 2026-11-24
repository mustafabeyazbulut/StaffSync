using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ContactsPage : ContentPage
    {
        private readonly ContactsViewModel _viewModel;

        public ContactsPage(ContactsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
