using StaffSync.Application.Interfaces.Directory;

namespace StaffSync.Infrastructure.DirectoryExport
{
    // Singleton. İlk değişikliğin zamanını tutar; dışa aktarım bitince o ana kadarki değişiklikleri temizler.
    public class DirectoryChangeNotifier : IDirectoryChangeNotifier
    {
        private long pendingTicks; // 0 = bekleyen değişiklik yok

        public void NotifyChanged() => Interlocked.CompareExchange(ref pendingTicks, DateTime.Now.Ticks, 0);

        public DateTime? PendingSince
        {
            get
            {
                long ticks = Interlocked.Read(ref pendingTicks);
                return ticks == 0 ? null : new DateTime(ticks);
            }
        }

        // Dışa aktarım sırasında yeni değişiklik geldiyse (zaman damgası değiştiyse) silinmez.
        public void Clear(DateTime pendingSnapshot) => Interlocked.CompareExchange(ref pendingTicks, 0, pendingSnapshot.Ticks);
    }
}
