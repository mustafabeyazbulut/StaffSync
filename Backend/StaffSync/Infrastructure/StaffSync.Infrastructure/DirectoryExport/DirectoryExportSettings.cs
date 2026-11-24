namespace StaffSync.Infrastructure.DirectoryExport
{
    // appsettings "DirectoryExport". PublishFolder boşsa servis hiçbir şey yazmaz.
    public class DirectoryExportSettings
    {
        public string PublishFolder { get; set; } = "";
        public string DailyAt { get; set; } = "06:00";
        public int DebounceMinutes { get; set; } = 5;
    }
}
