using StaffSync.Application.Features.Mappings;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class CorrectionMappingsTests
{
    [Fact]
    public void ToDto_includes_contact_name_and_labelled_items()
    {
        var correction = new ContactCorrection
        {
            Id = 3, ContactId = 12, Contact = new Contact { DisplayName = "Ahmet Yılmaz" }, Note = "n", Status = CorrectionStatus.New,
            Items = { new ContactCorrectionItem { FieldName = "Home", OldValue = "2145", NewValue = "2146" } }
        };

        var dto = correction.ToDto();

        Assert.Equal("Ahmet Yılmaz", dto.ContactName);
        Assert.Equal(CorrectionStatus.New, dto.Status);
        Assert.Equal("Kısa kod", dto.Items.Single().Label);
        Assert.Equal("2146", dto.Items.Single().NewValue);
    }
}
