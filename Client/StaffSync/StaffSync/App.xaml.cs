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

            // Durum çubuğu görünen sayfanın zeminini izler: sayfa göründüğünde ve tema değiştiğinde
            PageAppearing += (_, _) => RefreshStatusBar();
            RequestedThemeChanged += (_, _) => RefreshStatusBar();

            // Modal sayfalar Android'de ayrı pencerede açılır; pencere ekrana bağlanınca (Loaded) boya
            ModalPushed += (_, e) =>
            {
                if (e.Modal.IsLoaded)
                {
                    RefreshStatusBar();
                    return;
                }

                void OnLoaded(object? sender, EventArgs args)
                {
                    e.Modal.Loaded -= OnLoaded;
                    RefreshStatusBar();
                }
                e.Modal.Loaded += OnLoaded;
            };
            ModalPopped += (_, _) => RefreshStatusBar();

            // Açılışta Shell "startup" sayfasında (açılış ekranı görünümü) bekler; oturum durumuna göre
            // tek seferde doğru ekrana geçilir, rehber ekranı arada görünüp kaybolmaz.
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
                    await Shell.Current.GoToAsync("//main/contacts");

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

        /// <summary>Durum çubuğunu görünen sayfanın (ve açık modalların) zeminine göre yeniden boyar.</summary>
        public static void RefreshStatusBar()
        {
#if ANDROID
            MainThread.BeginInvokeOnMainThread(() =>
                (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as MainActivity)?.UpdateStatusBar());
#endif
        }
    }
}
