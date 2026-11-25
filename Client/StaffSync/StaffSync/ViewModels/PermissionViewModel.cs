using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StaffSync.Services;

namespace StaffSync.ViewModels
{
    public partial class PermissionViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IContactSyncService _syncService;

        public PermissionViewModel(ISettingsService settingsService, IContactSyncService syncService)
        {
            _settingsService = settingsService;
            _syncService = syncService;
        }

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        [RelayCommand]
        private async Task AllowSyncAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                _settingsService.HasCompletedPermissionIntro = true;

                // İzin isteği ana thread'de
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
                    if (Application.Current?.Windows[0].Page is Page page)
                    {
                        await page.DisplayAlertAsync(
                            "İzin Verilmedi",
                            "Rehber erişim izni verilmediği için şirket kişileri telefonunuza eklenemez. İstediğiniz zaman Profil sekmesinden senkronizasyonu açabilirsiniz.",
                            "Tamam");
                    }
                }
                else
                {
                    _settingsService.IsSyncEnabled = true;

                    // Arka planda ilk senkronizasyonu başlat
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _syncService.SyncContactsAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"İlk senkronizasyon hatası: {ex}");
                        }
                    });
                }

                await Shell.Current.GoToAsync("//main/contacts");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SkipAsync()
        {
            if (IsBusy)
                return;

            _settingsService.HasCompletedPermissionIntro = true;
            _settingsService.IsSyncEnabled = false;

            await Shell.Current.GoToAsync("//main/contacts");
        }
    }
}
