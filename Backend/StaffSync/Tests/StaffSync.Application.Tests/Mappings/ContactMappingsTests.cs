using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Mappings;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Mappings;

public class ContactMappingsTests
{
    private static Contact Sample() => new()
    {
        Id = 7, DisplayName = "Ayşe Kaya", ImageUrl = "img", Email = "ayse@x.com", TelephoneNumber = "+905320000000",
        Home = "2145", Office = "GM", JobTitle = "İK Uzmanı", Department = "İnsan Kaynakları", Company = "AUNDE", ManagerName = "Mehmet Kurt"
    };

    [Fact]
    public void ToDto_copies_every_field()
    {
        var dto = Sample().ToDto();
        Assert.Equal(7, dto.Id);
        Assert.Equal("Ayşe Kaya", dto.DisplayName);
        Assert.Equal("img", dto.ImageUrl);
        Assert.Equal("ayse@x.com", dto.Email);
        Assert.Equal("+905320000000", dto.TelephoneNumber);
        Assert.Equal("2145", dto.Home);
        Assert.Equal("GM", dto.Office);
        Assert.Equal("İK Uzmanı", dto.JobTitle);
        Assert.Equal("İnsan Kaynakları", dto.Department);
        Assert.Equal("AUNDE", dto.Company);
        Assert.Equal("Mehmet Kurt", dto.ManagerName);
    }

    [Fact]
    public void AsDto_expression_works_in_queries()
    {
        var list = new List<Contact> { Sample() }.AsQueryable().Select(ContactMappings.AsDto).ToList();
        Assert.Equal("Ayşe Kaya", list.Single().DisplayName);
    }

    [Fact]
    public void ApplyTo_overwrites_fields_but_keeps_id_and_created_date()
    {
        var contact = Sample();
        var created = contact.CreatedDate;
        new UpdateContactCommand
        {
            Id = 99, DisplayName = "Yeni Ad", ImageUrl = "i2", Email = "e2", TelephoneNumber = "t2", Home = "h2",
            Office = "o2", JobTitle = "j2", Department = "d2", Company = "c2", ManagerName = "m2"
        }.ApplyTo(contact);

        Assert.Equal(7, contact.Id);
        Assert.Equal(created, contact.CreatedDate);
        Assert.Equal("Yeni Ad", contact.DisplayName);
        Assert.Equal("m2", contact.ManagerName);
    }

    [Fact]
    public void ToEntity_builds_contact_from_create_command()
    {
        var c = new CreateContactCommand
        {
            DisplayName = "Can Öztürk", ImageUrl = "i", Email = "e", TelephoneNumber = "t", Home = "h",
            Office = "o", JobTitle = "j", Department = "d", Company = "c", ManagerName = "m"
        }.ToEntity();
        Assert.Equal("Can Öztürk", c.DisplayName);
        Assert.False(c.IsDeleted);
    }
}
