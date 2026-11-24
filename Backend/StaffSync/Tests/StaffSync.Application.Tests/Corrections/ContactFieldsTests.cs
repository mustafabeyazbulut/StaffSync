using StaffSync.Application.Features.Corrections;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class ContactFieldsTests
{
    private static readonly Contact C = new() { TelephoneNumber = "+905320000000", Home = "2145", JobTitle = null! };

    [Fact]
    public void Known_fields_are_accepted_unknown_rejected()
    {
        Assert.True(ContactFields.IsKnown("TelephoneNumber"));
        Assert.True(ContactFields.IsKnown(ContactFields.Other));
        Assert.False(ContactFields.IsKnown("PasswordHash"));
    }

    [Fact]
    public void Current_value_reads_contact_and_treats_null_as_empty()
    {
        Assert.Equal("+905320000000", ContactFields.CurrentValue(C, "TelephoneNumber"));
        Assert.Equal("", ContactFields.CurrentValue(C, "JobTitle"));
        Assert.Equal("", ContactFields.CurrentValue(C, ContactFields.Other));
    }

    [Fact]
    public void Labels_are_turkish() => Assert.Equal("Kısa kod", ContactFields.Label("Home"));
}
