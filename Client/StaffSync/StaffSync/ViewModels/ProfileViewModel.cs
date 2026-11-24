using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StaffSync.Models;
using StaffSync.Services;

namespace StaffSync.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;
        private readonly IContactSyncService _syncService;

        public ProfileViewModel(
            IAuthService authService,
            ISettingsService settingsService,
            IContactSyncService syncService)
        {
            _authService = authService;
            _settingsService = settingsService;
            _syncService = syncService;

            IsSyncEnabled = _settingsService.IsSyncEnabled;
            SelectedThemeIndex = (int)_settingsService.SelectedTheme;
        }

        [ObservableProperty]
        public partial UserInfo? CurrentUser { get; set; }

        [ObservableProperty]
        public partial bool IsSyncing { get; set; }

        [ObservableProperty]
        public partial string LastSyncText { get; set; } = "Henüz senkronize edilmedi";

        [ObservableProperty]
        public partial string? LastSyncError { get; set; }

        public bool HasSyncError => !string.IsNullOrEmpty(LastSyncError);

        [ObservableProperty]
        public partial bool IsSyncEnabled { get; set; }

        [ObservableProperty]
        public partial int SelectedThemeIndex { get; set; }

        public string AppVersion => AppInfo.Current.VersionString;

        public async Task InitializeAsync()
        {
            CurrentUser = await _authService.GetCurrentUserAsync();
            UpdateSyncStatusText();
        }

        private void UpdateSyncStatusText()
        {
            var lastTime = _settingsService.LastSyncTime;
            if (lastTime.HasValue)
            {
                var local = lastTime.Value.ToLocalTime();
                LastSyncText = $"Son senkron: {local:dd.MM.yyyy HH:mm}";
            }
            else
            {
                LastSyncText = "Henüz senkronize edilmedi";
            }

            LastSyncError = _settingsService.LastSyncError;
            OnPropertyChanged(nameof(HasSyncError));
        }

        [RelayCommand]
        public async Task ToggleSyncAsync(bool newValue)
        {
            if (newValue == _settingsService.IsSyncEnabled)
                return;

            if (!newValue)
            {
                // Kapatırken onay modalı sor (Spec kuralı)
                if (Application.Current?.Windows[0].Page is Page page)
                {
                    var confirmed = await page.DisplayAlertAsync(
                        "Rehber Senkronizasyonunu Kapat",
                        "StaffSync tarafından telefonunuza eklenen şirket kişileri rehberinizden temizlenecektir. Kişisel rehberinizdeki kişilere dokunulmaz.\n\nOnaylıyor musunuz?",
                        "Evet, Temizle ve Kapat",
                        "İptal");

                    if (!confirmed)
                    {
                        // Geri al
                        IsSyncEnabled = true;
                        return;
                    }
                }

                try
                {
                    IsSyncing = true;
                    await _syncService.ClearSyncedContactsAsync();
                    _settingsService.IsSyncEnabled = false;
                    IsSyncEnabled = false;
                    UpdateSyncStatusText();
                }
                finally
                {
                    IsSyncing = false;
                }
            }
            else
            {
                // Açarken senkron başlat
                _settingsService.IsSyncEnabled = true;
                IsSyncEnabled = true;
                await SyncNowAsync();
            }
        }

        [RelayCommand]
        public async Task SyncNowAsync()
        {
            if (IsSyncing)
                return;

            try
            {
                IsSyncing = true;
                var count = await _syncService.SyncContactsAsync();
                UpdateSyncStatusText();

                if (Application.Current?.Windows[0].Page is Page page)
                {
                    await page.DisplayAlertAsync("Senkronizasyon", $"{count} kişi başarıyla senkronize edildi.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = ex.Message;
                UpdateSyncStatusText();
            }
            finally
            {
                IsSyncing = false;
            }
        }

        [RelayCommand]
        public void SetTheme(string themeStr)
        {
            if (int.TryParse(themeStr, out var idx) && idx >= 0 && idx <= 2)
            {
                SelectedThemeIndex = idx;
                _settingsService.SelectedTheme = (ThemeOption)idx;
            }
        }

        [RelayCommand]
        public async Task LogoutAsync()
        {
            if (Application.Current?.Windows[0].Page is Page page)
            {
                var confirm = await page.DisplayAlertAsync(
                    "Çıkış Yap",
                    "Hesabınızdan çıkış yapmak istediğinize emin misiniz?",
                    "Evet, Çıkış Yap",
                    "İptal");

                if (confirm)
                {
                    await _authService.LogoutAsync();
                    await Shell.Current.GoToAsync("//login");
                }
            }
        }
    }
}
