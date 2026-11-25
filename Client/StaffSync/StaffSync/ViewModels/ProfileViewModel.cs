using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StaffSync.Messages;
using StaffSync.Models;
using StaffSync.Services;
using StaffSync.Utils;

namespace StaffSync.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;
        private readonly IContactSyncService _syncService;
        private readonly IContactCacheService _cacheService;
        private readonly IDialogService _dialogService;

        public ProfileViewModel(
            IAuthService authService,
            ISettingsService settingsService,
            IContactSyncService syncService,
            IContactCacheService cacheService,
            IDialogService dialogService)
        {
            _authService = authService;
            _settingsService = settingsService;
            _syncService = syncService;
            _cacheService = cacheService;
            _dialogService = dialogService;

            IsSyncEnabled = _settingsService.IsSyncEnabled;
            SelectedThemeIndex = (int)_settingsService.SelectedTheme;
            UsesCompanyLine = _settingsService.UsesCompanyLine;

            // Açılış / arka plan senkronu bu ekran açıkken biterse "Son senkron" anında güncellensin.
            WeakReferenceMessenger.Default.Register<SyncStateChangedMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsSyncing = _manualSyncInProgress || m.Value;
                    if (!m.Value)
                        UpdateSyncStatusText();
                });
            });
        }

        // "Şimdi senkronize et" ile başlatılan senkron; arka plan senkronundan ayrı tutulur ki
        // arka plan bittiğinde düğme tekrar basılabilir hale gelip ikinci bir uyarı çıkmasın.
        private bool _manualSyncInProgress;

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
        public partial string UserInitials { get; set; } = "";

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompanyLineDescription))]
        public partial bool UsesCompanyLine { get; set; }

        public string CompanyLineDescription => UsesCompanyLine
            ? "Aramalar kısa kodla yapılır"
            : "Aramalar cep numarasıyla yapılır";

        partial void OnUsesCompanyLineChanged(bool value)
        {
            if (value == _settingsService.UsesCompanyLine)
                return; // ilk yüklemede ayarın kendisi atanıyor

            _settingsService.UsesCompanyLine = value;
            WeakReferenceMessenger.Default.Send(new CompanyLineChangedMessage(value));

            // Telefon rehberindeki varsayılan numara da bu tercihe göre yazılır: biçim değişti,
            // senkron açıksa kişiler hemen yeniden yazılsın.
            if (_settingsService.IsSyncEnabled)
                _ = SyncNowAsync();
        }

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
                UserEmail = CurrentUser.Email;
                IsAdmin = CurrentUser.Role.Contains("admin", StringComparison.OrdinalIgnoreCase);

                // Önce hesap bilgisiyle hemen doldur; önbellek (senkron sürerken kilitli olabilir) okununca iyileştir
                UserDisplayName = DisplayText.UserDisplayName(CurrentUser, Array.Empty<ContactDto>());
                UserInitials = DisplayText.Initials(UserDisplayName);

                IReadOnlyList<ContactDto> cached = Array.Empty<ContactDto>();
                try
                {
                    cached = await _cacheService.GetCachedContactsAsync();
                }
                catch (Exception ex)
                {
                    // Önbellek okunamazsa profil yine de hesap adı/e-posta ile gösterilir
                    System.Diagnostics.Debug.WriteLine($"Profil önbellek okuma hatası: {ex}");
                }

                UserDisplayName = DisplayText.UserDisplayName(CurrentUser, cached);
                UserDepartment = DisplayText.FindSelf(CurrentUser, cached)?.Department;
                UserInitials = DisplayText.Initials(UserDisplayName);
                OnPropertyChanged(nameof(HasUserDepartment));
            }

            UpdateSyncStatusText();
            IsSyncing = _manualSyncInProgress || _syncService.IsSyncRunning;
        }

        private void UpdateSyncStatusText()
        {
            LastSyncText = DisplayText.LastSync(_settingsService.LastSyncTime) ?? "Henüz yapılmadı";

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
                var count = await _syncService.GetSyncedContactsCountAsync();
                var confirmed = await _dialogService.ConfirmAsync(
                    "Rehber aktarımı kapatılsın mı?",
                    $"AUNDE Rehber'in telefonunuza eklediği {count} şirket kişisi rehberinizden kaldırılacak. Kendi kişilerinize dokunulmaz.",
                    "Kapat ve kaldır",
                    destructive: true);

                if (!confirmed)
                {
                    IsSyncEnabled = true;
                    return;
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
                    await _dialogService.AlertAsync(
                        "Rehber izni gerekiyor",
                        "Şirket kişilerini telefonunuza ekleyebilmemiz için rehber izni gerekiyor. İzni dilediğiniz zaman telefonun Ayarlar > Uygulamalar > AUNDE Rehber bölümünden verebilirsiniz.");
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
            if (_manualSyncInProgress)
                return;

            try
            {
                _manualSyncInProgress = true;
                IsSyncing = true;
                // Arka planda bir senkron sürüyorsa servis onun bitmesini bekler, sonra artımlı senkron yapar.
                var count = await _syncService.SyncContactsAsync();
                UpdateSyncStatusText();

                if (!string.IsNullOrEmpty(_settingsService.LastSyncError))
                    await ShowSyncErrorAsync(_settingsService.LastSyncError);
                else if (count == 0)
                    await _dialogService.ToastAsync("Rehberiniz güncel");
                else
                    await _dialogService.ToastAsync($"{count} kişi güncellendi");
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = ex.Message;
                UpdateSyncStatusText();
                await ShowSyncErrorAsync(ex.Message);
            }
            finally
            {
                _manualSyncInProgress = false;
                IsSyncing = _syncService.IsSyncRunning;
            }
        }

        private Task ShowSyncErrorAsync(string detail) =>
            _dialogService.AlertAsync(
                "Rehber güncellenemedi",
                $"{detail}\n\nBiraz sonra tekrar deneyebilirsiniz.",
                DialogKind.Error);

        [RelayCommand]
        public async Task LogoutAsync()
        {
            var confirm = await _dialogService.ConfirmAsync(
                "Çıkış yapılsın mı?",
                "Tekrar giriş yapana kadar şirket rehberini göremezsiniz.",
                "Çıkış yap");

            if (confirm)
            {
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//login");
            }
        }
    }
}
