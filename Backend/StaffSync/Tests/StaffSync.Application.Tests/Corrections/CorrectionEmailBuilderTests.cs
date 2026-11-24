using StaffSync.Application.Features.Corrections;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class CorrectionEmailBuilderTests
{
    [Fact]
    public void Email_lists_old_and_new_values_and_note()
    {
        var contact = new Contact { Id = 12, DisplayName = "Ahmet Yılmaz" };
        var items = new[]
        {
            new ContactCorrectionItem { FieldName = "TelephoneNumber", OldValue = "111", NewValue = "222" },
            new ContactCorrectionItem { FieldName = "Office", OldValue = "", NewValue = "Fabrika" }
        };

        var (subject, body) = CorrectionEmailBuilder.Build(contact, "Ayşe Kaya", "ayse@x.com", items, "Ocakta taşındı");

        Assert.Equal("StaffSync düzeltme önerisi: Ahmet Yılmaz", subject);
        Assert.Contains("Ayşe Kaya (ayse@x.com)", body);
        Assert.Contains("Ahmet Yılmaz (#12)", body);
        Assert.Contains("- Cep telefonu: 111 → 222", body);
        Assert.Contains("- Ofis: (boş) → Fabrika", body);
        Assert.Contains("Not: Ocakta taşındı", body);
    }

    [Fact]
    public void Note_line_is_omitted_when_empty()
    {
        var (_, body) = CorrectionEmailBuilder.Build(new Contact { DisplayName = "X" }, "A", "a@x", Array.Empty<ContactCorrectionItem>(), null);
        Assert.DoesNotContain("Not:", body);
    }
}
