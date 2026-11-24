using StaffSync.Application.Features.DirectoryExport;

namespace StaffSync.Application.Tests.DirectoryExport;

public class ExportScheduleTests
{
    private static readonly TimeOnly SixAm = new(6, 0);
    private static readonly TimeSpan Debounce = TimeSpan.FromMinutes(5);
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    [Fact]
    public void First_run_always_exports() =>
        Assert.True(ExportSchedule.ShouldExport(At(24, 10), lastExport: null, pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Daily_time_passed_since_last_export() =>
        Assert.True(ExportSchedule.ShouldExport(At(24, 6, 1), lastExport: At(23, 6, 0), pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Before_daily_time_nothing_happens() =>
        Assert.False(ExportSchedule.ShouldExport(At(24, 5, 59), lastExport: At(23, 6, 0), pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Already_exported_today_after_daily_time() =>
        Assert.False(ExportSchedule.ShouldExport(At(24, 12), lastExport: At(24, 6, 0), pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Change_waits_for_debounce() =>
        Assert.False(ExportSchedule.ShouldExport(At(24, 12, 4), lastExport: At(24, 6), pendingSince: At(24, 12, 0), SixAm, Debounce));

    [Fact]
    public void Change_exports_after_debounce() =>
        Assert.True(ExportSchedule.ShouldExport(At(24, 12, 5), lastExport: At(24, 6), pendingSince: At(24, 12, 0), SixAm, Debounce));
}
