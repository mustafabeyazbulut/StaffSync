using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileViewModel _viewModel;

        public ProfilePage(ProfileViewModel viewModel)
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

        private async void OnSyncSwitchToggled(object? sender, ToggledEventArgs e)
        {
            await _viewModel.ToggleSyncCommand.ExecuteAsync(e.Value);
        }
    }
}
