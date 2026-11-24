using StaffSync.Services;

namespace StaffSync
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;

        public App(IAuthService authService, ISettingsService settingsService)
        {
            InitializeComponent();
            _authService = authService;
            _settingsService = settingsService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            _settingsService.ApplyTheme();

            window.Created += async (s, e) =>
            {
                var isAuthenticated = await _authService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    await Shell.Current.GoToAsync("//login");
                }
                else if (!_settingsService.HasCompletedPermissionIntro)
                {
                    await Shell.Current.GoToAsync("//permission-intro");
                }
            };

            return window;
        }
    }
}
