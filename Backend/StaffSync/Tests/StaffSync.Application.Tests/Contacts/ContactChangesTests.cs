using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Contacts;

public class ContactChangesTests
{
    private static readonly DateTime T0 = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static IQueryable<Contact> Data() => new List<Contact>
    {
        new() { Id = 1, DisplayName = "Eski", UpdatedDate = T0.AddDays(-1) },
        new() { Id = 2, DisplayName = "Yeni", UpdatedDate = T0.AddHours(1) },
        new() { Id = 3, DisplayName = "Eskiden silinmiş", IsDeleted = true, UpdatedDate = T0.AddDays(-1) },
        new() { Id = 4, DisplayName = "Yeni silinmiş", IsDeleted = true, UpdatedDate = T0.AddHours(2) },
    }.AsQueryable();

    [Fact]
    public void Without_since_returns_all_active_and_no_deletions()
    {
        Assert.Equal(new[] { 1, 2 }, ContactChanges.Upserted(Data(), null).Select(c => c.Id).OrderBy(x => x).ToArray());
        Assert.Empty(ContactChanges.DeletedIds(Data(), null).ToList());
    }

    [Fact]
    public void With_since_returns_only_changes_after_it()
    {
        Assert.Equal(new[] { 2 }, ContactChanges.Upserted(Data(), T0).Select(c => c.Id).ToArray());
        Assert.Equal(new[] { 4 }, ContactChanges.DeletedIds(Data(), T0).ToArray());
    }
}
