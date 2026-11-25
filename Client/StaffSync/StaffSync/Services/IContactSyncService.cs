namespace StaffSync.Services
{
    public interface IContactSyncService
    {
        bool IsSupported { get; }

        /// <summary>Şu anda bir senkron çalışıyor mu (açılış / arka plan senkronu dahil).</summary>
        bool IsSyncRunning { get; }
        Task<int> SyncContactsAsync(CancellationToken ct = default);
        Task ClearSyncedContactsAsync(CancellationToken ct = default);
        Task<int> GetSyncedContactsCountAsync(CancellationToken ct = default);
    }
}
