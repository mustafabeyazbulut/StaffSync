using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ContactsPage : ContentPage
    {
        private readonly ContactsViewModel _viewModel;

        public ContactsPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<ContactsViewModel>()!)
        {
        }

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

        private void OnLetterTapped(object? sender, EventArgs e)
        {
            if (sender is BindableObject bindable && bindable.BindingContext is string letter)
            {
                var group = _viewModel.GroupedContacts.FirstOrDefault(g => g.Key.Equals(letter, StringComparison.OrdinalIgnoreCase));
                if (group != null)
                {
                    GroupedListView.ScrollTo(group, position: ScrollToPosition.Start, animate: false);
                }
            }
        }
    }
}
