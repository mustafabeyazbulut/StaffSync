using StaffSync.Models;

namespace StaffSync.Services
{
    public interface IContactCacheService
    {
        Task<List<ContactDto>> GetCachedContactsAsync();
        Task RefreshCacheAsync(CancellationToken ct = default);
        Task UpdateCacheWithChangesAsync(ContactChangesResult changes);
        DateTime? GetCacheLastSyncTime();
    }

    public class ContactCacheService : IContactCacheService
    {
        private const string CacheSinceKey = "StaffSync_OfflineCache_Since";
        private const string CacheFileName = "offline_contacts_cache.json";

        private readonly IContactsService _contactsService;
        private readonly string _cacheFilePath = Path.Combine(FileSystem.AppDataDirectory, CacheFileName);
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        public ContactCacheService(IContactsService contactsService)
        {
            _contactsService = contactsService;
        }

        public DateTime? GetCacheLastSyncTime()
        {
            var val = Preferences.Default.Get(CacheSinceKey, string.Empty);
            if (DateTime.TryParse(val, out var dt))
                return dt;
            return null;
        }

        public async Task<List<ContactDto>> GetCachedContactsAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_cacheFilePath))
                    return new List<ContactDto>();

                var json = await File.ReadAllTextAsync(_cacheFilePath);
                return System.Text.Json.JsonSerializer.Deserialize<List<ContactDto>>(json) ?? new List<ContactDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCachedContactsAsync okuma hatası: {ex}");
                return new List<ContactDto>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task RefreshCacheAsync(CancellationToken ct = default)
        {
            try
            {
                var since = GetCacheLastSyncTime();
                var changes = await _contactsService.GetChangesAsync(since);
                if (changes != null)
                {
                    await UpdateCacheWithChangesAsync(changes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshCacheAsync hatası: {ex}");
            }
        }

        public async Task UpdateCacheWithChangesAsync(ContactChangesResult changes)
        {
            await _fileLock.WaitAsync();
            try
            {
                List<ContactDto> cached;
                var since = GetCacheLastSyncTime();

                if (since == null || !File.Exists(_cacheFilePath))
                {
                    cached = changes.Upserted.ToList();
                }
                else
                {
                    var json = await File.ReadAllTextAsync(_cacheFilePath);
                    cached = System.Text.Json.JsonSerializer.Deserialize<List<ContactDto>>(json) ?? new List<ContactDto>();

                    if (changes.DeletedIds != null && changes.DeletedIds.Count > 0)
                    {
                        cached.RemoveAll(c => changes.DeletedIds.Contains(c.Id));
                    }

                    foreach (var item in changes.Upserted)
                    {
                        var idx = cached.FindIndex(c => c.Id == item.Id);
                        if (idx >= 0)
                            cached[idx] = item;
                        else
                            cached.Add(item);
                    }
                }

                var outputJson = System.Text.Json.JsonSerializer.Serialize(cached);
                await File.WriteAllTextAsync(_cacheFilePath, outputJson);
                Preferences.Default.Set(CacheSinceKey, changes.ServerTimeUtc.ToString("O"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCacheWithChangesAsync hatası: {ex}");
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
