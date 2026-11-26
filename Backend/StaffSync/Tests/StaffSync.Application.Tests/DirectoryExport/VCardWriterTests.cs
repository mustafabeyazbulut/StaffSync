using StaffSync.Application.Features.DirectoryExport;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.DirectoryExport;

public class VCardWriterTests
{
    [Fact]
    public void Ascii_contact_is_written_plain()
    {
        var c = new Contact { DisplayName = "Burak Demir", TelephoneNumber = "+905320000001", Home = "2001", Email = "burak@x.com",
                              Company = "ACME", Department = "IT", JobTitle = "Sistem Uzmani" };

        string expected =
            "BEGIN:VCARD\r\n" +
            "VERSION:2.1\r\n" +
            "N:Demir;Burak\r\n" +
            "FN:Burak Demir\r\n" +
            "TEL;CELL:+905320000001\r\n" +
            "TEL;WORK:2001\r\n" +
            "EMAIL;INTERNET:burak@x.com\r\n" +
            "ORG:ACME;IT\r\n" +
            "TITLE:Sistem Uzmani\r\n" +
            "END:VCARD\r\n";

        Assert.Equal(expected, VCardWriter.Write(new[] { c }));
    }

    [Fact]
    public void Turkish_values_are_quoted_printable()
    {
        string v = VCardWriter.Write(new[] { new Contact { DisplayName = "Ayşe Kaya", TelephoneNumber = "1" } });
        Assert.Contains("N;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:Kaya;Ay=C5=9Fe\r\n", v);
        Assert.Contains("FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:Ay=C5=9Fe=20Kaya\r\n", v);
    }

    [Fact]
    public void Empty_fields_are_skipped_and_single_word_name_goes_to_first_name()
    {
        string v = VCardWriter.Write(new[] { new Contact { DisplayName = "Resepsiyon", TelephoneNumber = "1", Home = null!, Email = "", Company = null!, Department = null! } });
        Assert.Contains("N:;Resepsiyon\r\n", v);
        Assert.DoesNotContain("TEL;WORK", v);
        Assert.DoesNotContain("EMAIL", v);
        Assert.DoesNotContain("ORG", v);
    }

    [Fact]
    public void Semicolons_in_structured_values_are_escaped() =>
        Assert.Contains("ORG:A\\;B;C\r\n", VCardWriter.Write(new[] { new Contact { DisplayName = "X", Company = "A;B", Department = "C" } }));

    [Fact]
    public void Multiple_contacts_are_concatenated() =>
        Assert.Equal(2, VCardWriter.Write(new[] { new Contact { DisplayName = "A" }, new Contact { DisplayName = "B" } })
            .Split("BEGIN:VCARD").Length - 1);
}
