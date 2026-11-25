using StaffSync.Services;

namespace StaffSync
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;

        public App(IAuthService authService, ISettingsService settingsService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _authService = authService;
            _settingsService = settingsService;
            _serviceProvider = serviceProvider;
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
                else
                {
                    // Oturum varsa arka planda: önbellek tazeleme + senkron açıksa telefon senkronu
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var cacheService = _serviceProvider.GetService<IContactCacheService>();
                            if (cacheService != null)
                            {
                                await cacheService.RefreshCacheAsync();
                            }

                            if (_settingsService.IsSyncEnabled)
                            {
                                var syncService = _serviceProvider.GetService<IContactSyncService>();
                                if (syncService != null && syncService.IsSupported)
                                {
                                    await syncService.SyncContactsAsync();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Açılış senkron hatası: {ex}");
                        }
                    });
                }
            };

            return window;
        }
    }
}
