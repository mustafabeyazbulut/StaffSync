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
                _settingsService.IsSyncEnabled = true;

                // Arka planda ilk senkronizasyonu başlat
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _syncService.SyncContactsAsync();
                    }
                    catch
                    {
                        // Hata ayarlar servisinde güncellenir
                    }
                });

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
