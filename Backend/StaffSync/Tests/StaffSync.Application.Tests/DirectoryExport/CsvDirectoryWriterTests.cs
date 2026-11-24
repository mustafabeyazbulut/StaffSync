using StaffSync.Application.Features.DirectoryExport;
using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Tests.DirectoryExport;

public class CsvDirectoryWriterTests
{
    [Fact]
    public void Starts_with_utf8_bom_and_turkish_header()
    {
        byte[] bytes = CsvDirectoryWriter.Write(Array.Empty<Contact>());
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        Assert.StartsWith("Ad Soyad;Cep Telefonu;Kısa Kod;E-posta;Unvan;Departman;Şirket;Ofis;Yönetici", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
    }

    [Fact]
    public void Fields_with_separator_or_quote_are_quoted()
    {
        var c = new Contact { DisplayName = "Ayşe \"AK\" Kaya", TelephoneNumber = "1", Home = "", Email = "", JobTitle = "A;B",
                              Department = "", Company = "", Office = "", ManagerName = null! };
        string text = Encoding.UTF8.GetString(CsvDirectoryWriter.Write(new[] { c })[3..]);
        Assert.Contains("\r\n\"Ayşe \"\"AK\"\" Kaya\";1;;;\"A;B\";;;;", text);
    }
}
