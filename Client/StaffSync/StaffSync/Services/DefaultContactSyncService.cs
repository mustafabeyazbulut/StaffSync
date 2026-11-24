namespace StaffSync.Services
{
    public class DefaultContactSyncService : IContactSyncService
    {
        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;

        public DefaultContactSyncService(IContactsService contactsService, ISettingsService settingsService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;
        }

        public virtual bool IsSupported => false;

        public virtual async Task<int> SyncContactsAsync(CancellationToken ct = default)
        {
            // Desteklenmeyen platformlarda (ör. Windows masaüstü) API'den değişiklikleri çekip
            // yerel önbelleğe kaydeder ve başarılı döner.
            var changes = await _contactsService.GetChangesAsync(_settingsService.LastSyncTime);
            if (changes != null)
            {
                var current = await _settingsService.GetCachedContactsAsync();
                
                // Silinenleri çıkar
                current.RemoveAll(c => changes.DeletedIds.Contains(c.Id));

                // Eklenen/güncellenenleri güncelle
                foreach (var item in changes.Upserted)
                {
                    var idx = current.FindIndex(c => c.Id == item.Id);
                    if (idx >= 0)
                        current[idx] = item;
                    else
                        current.Add(item);
                }

                await _settingsService.SaveCachedContactsAsync(current);
                _settingsService.LastSyncTime = changes.ServerTimeUtc;
                _settingsService.LastSyncError = null;

                return changes.Upserted.Count;
            }

            return 0;
        }

        public virtual Task ClearSyncedContactsAsync(CancellationToken ct = default)
        {
            _settingsService.LastSyncTime = null;
            _settingsService.LastSyncError = null;
            return Task.CompletedTask;
        }
    }
}
