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
        private readonly IContactCacheService _cacheService;

        public ProfileViewModel(
            IAuthService authService,
            ISettingsService settingsService,
            IContactSyncService syncService,
            IContactCacheService cacheService)
        {
            _authService = authService;
            _settingsService = settingsService;
            _syncService = syncService;
            _cacheService = cacheService;

            IsSyncEnabled = _settingsService.IsSyncEnabled;
            SelectedThemeIndex = (int)_settingsService.SelectedTheme;
        }

        [ObservableProperty]
        public partial UserInfo? CurrentUser { get; set; }

        [ObservableProperty]
        public partial string UserDisplayName { get; set; } = "";

        [ObservableProperty]
        public partial string UserEmail { get; set; } = "";

        [ObservableProperty]
        public partial string? UserDepartment { get; set; }

        public bool HasUserDepartment => !string.IsNullOrWhiteSpace(UserDepartment);

        [ObservableProperty]
        public partial string UserInitials { get; set; } = "??";

        [ObservableProperty]
        public partial bool IsAdmin { get; set; }

        [ObservableProperty]
        public partial bool IsSyncing { get; set; }

        [ObservableProperty]
        public partial string LastSyncText { get; set; } = "Henüz yapılmadı";

        [ObservableProperty]
        public partial string? LastSyncError { get; set; }

        public bool HasSyncError => !string.IsNullOrEmpty(LastSyncError);

        [ObservableProperty]
        public partial bool IsSyncEnabled { get; set; }

        [ObservableProperty]
        public partial int SelectedThemeIndex { get; set; }

        public string AppVersion => AppInfo.Current.VersionString;

        partial void OnSelectedThemeIndexChanged(int value)
        {
            if (value >= 0 && value <= 2)
            {
                _settingsService.SelectedTheme = (ThemeOption)value;
                Application.Current!.UserAppTheme = value switch
                {
                    1 => AppTheme.Light,
                    2 => AppTheme.Dark,
                    _ => AppTheme.Unspecified
                };
            }
        }

        public async Task InitializeAsync()
        {
            CurrentUser = await _authService.GetCurrentUserAsync();
            if (CurrentUser != null)
            {
                UserEmail = CurrentUser.Email ?? "";
                UserDisplayName = CurrentUser.Name ?? "";

                IsAdmin = !string.IsNullOrWhiteSpace(CurrentUser.Role) &&
                          CurrentUser.Role.Contains("admin", StringComparison.OrdinalIgnoreCase);

                try
                {
                    var cached = await _cacheService.GetCachedContactsAsync();
                    var match = cached.FirstOrDefault(c =>
                        !string.IsNullOrWhiteSpace(c.Email) &&
                        string.Equals(c.Email.Trim(), UserEmail.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        if (!string.IsNullOrWhiteSpace(match.DisplayName))
                            UserDisplayName = match.DisplayName;
                        UserDepartment = match.Department;
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(UserDisplayName))
                {
                    UserDisplayName = UserEmail.Split('@')[0];
                }

                UserInitials = ComputeInitials(UserDisplayName);
                OnPropertyChanged(nameof(HasUserDepartment));
            }

            UpdateSyncStatusText();
        }

        private static string ComputeInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "??";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        private void UpdateSyncStatusText()
        {
            var lastTime = _settingsService.LastSyncTime;
            if (lastTime.HasValue)
            {
                var local = lastTime.Value.ToLocalTime();
                var today = DateTime.Today;
                if (local.Date == today)
                    LastSyncText = $"Bugün {local:HH:mm}";
                else if (local.Date == today.AddDays(-1))
                    LastSyncText = $"Dün {local:HH:mm}";
                else
                    LastSyncText = $"{local:dd.MM.yyyy HH:mm}";
            }
            else
            {
                LastSyncText = "Henüz yapılmadı";
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
                if (Application.Current?.Windows[0].Page is Page page)
                {
                    var count = await _syncService.GetSyncedContactsCountAsync();
                    var confirmed = await page.DisplayAlertAsync(
                        "Rehber Senkronizasyonunu Kapat",
                        $"StaffSync tarafından telefonunuza eklenen {count} şirket kişisi rehberinizden kaldırılacaktır. Kişisel rehberinizdeki kişilere dokunulmaz.\n\nOnaylıyor musunuz?",
                        "Evet, Temizle ve Kapat",
                        "İptal");

                    if (!confirmed)
                    {
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
                var status = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var writeStatus = await Permissions.CheckStatusAsync<Permissions.ContactsWrite>();
                    if (writeStatus != PermissionStatus.Granted)
                    {
                        writeStatus = await Permissions.RequestAsync<Permissions.ContactsWrite>();
                    }

                    var readStatus = await Permissions.CheckStatusAsync<Permissions.ContactsRead>();
                    if (readStatus != PermissionStatus.Granted)
                    {
                        readStatus = await Permissions.RequestAsync<Permissions.ContactsRead>();
                    }

                    return (writeStatus == PermissionStatus.Granted && readStatus == PermissionStatus.Granted)
                        ? PermissionStatus.Granted
                        : PermissionStatus.Denied;
                });

                if (status != PermissionStatus.Granted)
                {
                    _settingsService.IsSyncEnabled = false;
                    IsSyncEnabled = false;
                    if (Application.Current?.Windows[0].Page is Page page)
                    {
                        await page.DisplayAlertAsync(
                            "İzin Verilmedi",
                            "Rehber okuma/yazma izni verilmediği için senkronizasyon açılamadı. Lütfen cihaz ayarlarından StaffSync uygulamasına rehber izni veriniz.",
                            "Tamam");
                    }
                    return;
                }

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
                    if (!string.IsNullOrEmpty(_settingsService.LastSyncError))
                    {
                        await page.DisplayAlertAsync("Senkronizasyon Hatası", _settingsService.LastSyncError, "Tamam");
                    }
                    else
                    {
                        await page.DisplayAlertAsync("Senkronizasyon", $"{count} kişi başarıyla senkronize edildi.", "Tamam");
                    }
                }
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = ex.Message;
                UpdateSyncStatusText();
                if (Application.Current?.Windows[0].Page is Page page)
                {
                    await page.DisplayAlertAsync("Senkronizasyon Hatası", ex.Message, "Tamam");
                }
            }
            finally
            {
                IsSyncing = false;
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
