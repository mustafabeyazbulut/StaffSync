using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Contacts;

public class ContactQueryBuilderTests
{
    private static IQueryable<Contact> Data() => new List<Contact>
    {
        new() { Id = 1, DisplayName = "Burak Demir", JobTitle = "Sistem Uzmanı", Department = "Bilgi Teknolojileri", Company = "AUNDE", TelephoneNumber = "111", Home = "2001", Email = "burak@x.com" },
        new() { Id = 2, DisplayName = "Ayşe Kaya", JobTitle = "İK Uzmanı", Department = "İnsan Kaynakları", Company = "AUNDE", TelephoneNumber = "222", Home = "2002", Email = "ayse@x.com" },
        new() { Id = 3, DisplayName = "Ahmet Yılmaz", JobTitle = "Satış Müdürü", Department = "Satış", Company = "Diğer AŞ", TelephoneNumber = "333", Home = "2003", Email = "ahmet@x.com" },
        new() { Id = 4, DisplayName = "Silinmiş Kişi", JobTitle = "X", Department = "Satış", Company = "AUNDE", TelephoneNumber = "444", Home = "2004", Email = "s@x.com", IsDeleted = true },
        new() { Id = 5, DisplayName = "Ahmet Yılmaz", JobTitle = null!, Department = null!, Company = null!, TelephoneNumber = null!, Home = null!, Email = null! },
    }.AsQueryable();

    [Fact]
    public void Deleted_contacts_are_never_returned() =>
        Assert.DoesNotContain(Data().ApplyFilters(null, null, null).ToList(), c => c.Id == 4);

    [Theory]
    [InlineData("Ayşe", 2)]       // ad
    [InlineData("Satış Müd", 3)]  // unvan
    [InlineData("İnsan", 2)]      // departman
    [InlineData("burak@", 1)]     // e-posta
    [InlineData("333", 3)]        // telefon
    [InlineData("2002", 2)]       // kısa kod
    public void Search_matches_any_field(string search, int expectedId) =>
        Assert.Equal(new[] { expectedId }, Data().ApplyFilters(search, null, null).Select(c => c.Id).ToArray());

    [Fact]
    public void Search_survives_null_fields() =>
        Assert.Empty(Data().ApplyFilters("yok-boyle-bir-sey", null, null).ToList());

    [Fact]
    public void Multiple_departments_are_ORed() =>
        Assert.Equal(new[] { 1, 3 }, Data().ApplyFilters(null, new[] { "Bilgi Teknolojileri", "Satış" }, null).Select(c => c.Id).OrderBy(x => x).ToArray());

    [Fact]
    public void Company_filter_is_exact() =>
        Assert.Equal(new[] { 3 }, Data().ApplyFilters(null, null, "Diğer AŞ").Select(c => c.Id).ToArray());

    [Fact]
    public void Sort_by_name_breaks_ties_by_id() =>
        Assert.Equal(new[] { 3, 5, 2, 1 }, Data().ApplyFilters(null, null, null).ApplySort(ContactSortBy.Name).Select(c => c.Id).ToArray());

    [Fact]
    public void Sort_by_department_then_name() =>
        Assert.Equal(new[] { 5, 1, 2, 3 }, Data().ApplyFilters(null, null, null).ApplySort(ContactSortBy.Department).Select(c => c.Id).ToArray());

    [Fact]
    public void Page_skips_and_takes() =>
        Assert.Equal(new[] { 2, 1 }, Data().ApplyFilters(null, null, null).ApplySort(ContactSortBy.Name).Page(2, 2).Select(c => c.Id).ToArray());
}
