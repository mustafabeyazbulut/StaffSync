using StaffSync.Models;

namespace StaffSync.Services
{
    public class DefaultContactSyncService : IContactSyncService
    {
        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;
        private readonly IContactCacheService _contactCacheService;

        public DefaultContactSyncService(
            IContactsService contactsService,
            ISettingsService settingsService,
            IContactCacheService contactCacheService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;
            _contactCacheService = contactCacheService;
        }

        public virtual bool IsSupported => false;

        public virtual bool IsSyncRunning => false;

        public virtual async Task<int> SyncContactsAsync(CancellationToken ct = default)
        {
            var changes = await _contactsService.GetChangesAsync(_settingsService.LastSyncTime);
            if (changes != null)
            {
                await _contactCacheService.UpdateCacheWithChangesAsync(changes);
                _settingsService.LastSyncTime = changes.ServerTimeUtc;
                _settingsService.LastSyncError = null;

                return changes.Upserted.Count;
            }

            return 0;
        }

        public virtual Task<int> GetSyncedContactsCountAsync(CancellationToken ct = default)
        {
            return Task.FromResult(0);
        }

        public virtual Task ClearSyncedContactsAsync(CancellationToken ct = default)
        {
            _settingsService.LastSyncTime = null;
            _settingsService.LastSyncError = null;
            return Task.CompletedTask;
        }
    }
}
