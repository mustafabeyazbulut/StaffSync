namespace StaffSync.Application.Interfaces.Directory
{
    // Kişi eklendi/güncellendi/silindi sinyali; arka plan servisi birkaç dakika sonra dosyaları yeniden yazar.
    public interface IDirectoryChangeNotifier
    {
        void NotifyChanged();
        DateTime? PendingSince { get; }
        void Clear(DateTime pendingSnapshot);
    }
}
