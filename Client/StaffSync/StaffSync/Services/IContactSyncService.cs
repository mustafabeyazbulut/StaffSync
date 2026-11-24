namespace StaffSync.Services
{
    public interface IContactSyncService
    {
        bool IsSupported { get; }
        Task<int> SyncContactsAsync(CancellationToken ct = default);
        Task ClearSyncedContactsAsync(CancellationToken ct = default);
    }
}
