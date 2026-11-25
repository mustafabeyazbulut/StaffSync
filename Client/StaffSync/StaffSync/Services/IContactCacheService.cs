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
                return await ReadCacheUnlockedAsync() ?? new List<ContactDto>();
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
                if (since != null)
                {
                    // Önbellek dosyası yok/bozuksa artımlı değişiklik birleştirilemez: baştan tam çek
                    await _fileLock.WaitAsync(ct);
                    try
                    {
                        if (await ReadCacheUnlockedAsync() == null)
                        {
                            ResetCacheUnlocked();
                            since = null;
                        }
                    }
                    finally
                    {
                        _fileLock.Release();
                    }
                }

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
                var isIncremental = GetCacheLastSyncTime() != null;
                var cached = isIncremental ? await ReadCacheUnlockedAsync() : null;

                if (isIncremental && cached == null)
                {
                    // Bozuk/eksik önbelleğe artımlı değişiklik eklenirse liste eksik kalır.
                    // Sıfırla; bir sonraki RefreshCacheAsync tam listeyi çeker.
                    ResetCacheUnlocked();
                    return;
                }

                if (cached == null)
                {
                    // Tam liste: sunucudan geleni olduğu gibi al
                    cached = changes.Upserted.ToList();
                }
                else
                {
                    if (changes.DeletedIds is { Count: > 0 })
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

                // Atomik yazma: önce geçici dosyaya, sonra yerine taşı. Doğrudan yazarken uygulama
                // kapatılırsa dosya boş kalıyor ve önbellek kalıcı olarak bozuluyordu.
                var tempPath = _cacheFilePath + ".tmp";
                await File.WriteAllTextAsync(tempPath, System.Text.Json.JsonSerializer.Serialize(cached));
                File.Move(tempPath, _cacheFilePath, overwrite: true);
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

        /// <summary>Önbellek dosyasını okur; dosya yoksa veya okunamıyorsa (boş/bozuk) null. Kilit çağırana ait.</summary>
        private async Task<List<ContactDto>?> ReadCacheUnlockedAsync()
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                return System.Text.Json.JsonSerializer.Deserialize<List<ContactDto>>(json);
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException || ex is IOException)
            {
                System.Diagnostics.Debug.WriteLine($"Önbellek okunamadı, sıfırlanacak: {ex.Message}");
                return null;
            }
        }

        /// <summary>Önbelleği siler ve "son güncelleme" işaretini kaldırır; sonraki yenileme tam liste çeker. Kilit çağırana ait.</summary>
        private void ResetCacheUnlocked()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                    File.Delete(_cacheFilePath);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Önbellek dosyası silinemedi: {ex.Message}");
            }
            Preferences.Default.Remove(CacheSinceKey);
        }
    }
}
