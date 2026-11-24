using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Contacts;

public class ContactFilterOptionsTests
{
    private static IQueryable<Contact> Data() => new List<Contact>
    {
        new() { Id = 1, Department = "Satış", Company = "AUNDE" },
        new() { Id = 2, Department = "Satış", Company = "AUNDE" },
        new() { Id = 3, Department = "Bilgi Teknolojileri", Company = "Diğer AŞ" },
        new() { Id = 4, Department = "Üretim", Company = "AUNDE", IsDeleted = true },
        new() { Id = 5, Department = "", Company = null! },
    }.AsQueryable();

    [Fact]
    public void Departments_are_counted_without_deleted_or_empty()
    {
        var result = ContactFilterOptions.Departments(Data()).ToList();
        Assert.Equal(new[] { ("Bilgi Teknolojileri", 1), ("Satış", 2) }, result.Select(x => (x.Name, x.Count)).ToArray());
    }

    [Fact]
    public void Companies_are_counted()
    {
        var result = ContactFilterOptions.Companies(Data()).ToList();
        Assert.Equal(new[] { ("AUNDE", 2), ("Diğer AŞ", 1) }, result.Select(x => (x.Name, x.Count)).ToArray());
    }
}
