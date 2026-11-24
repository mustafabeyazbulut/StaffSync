namespace StaffSync.Application.Features.DirectoryExport
{
    public static class ExportSchedule
    {
        // now, lastExport, pendingSince: sunucunun yerel saati.
        public static bool ShouldExport(DateTime now, DateTime? lastExport, DateTime? pendingSince, TimeOnly dailyAt, TimeSpan debounce)
        {
            if (lastExport is null) return true;

            DateTime todaysRun = now.Date + dailyAt.ToTimeSpan();
            if (now >= todaysRun && lastExport < todaysRun) return true;

            return pendingSince is not null && now - pendingSince >= debounce;
        }
    }
}
