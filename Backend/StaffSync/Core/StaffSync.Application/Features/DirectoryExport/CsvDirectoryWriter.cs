using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    // Türkçe Excel'in doğrudan açtığı biçim: UTF-8 BOM + ";" ayırıcı.
    public static class CsvDirectoryWriter
    {
        private static readonly string[] Header =
            { "Ad Soyad", "Cep Telefonu", "Kısa Kod", "E-posta", "Unvan", "Departman", "Şirket", "Ofis", "Yönetici" };

        public static byte[] Write(IEnumerable<Contact> contacts)
        {
            var sb = new StringBuilder().Append(string.Join(';', Header));
            foreach (Contact c in contacts)
            {
                sb.Append("\r\n").Append(string.Join(';', new[]
                {
                    c.DisplayName, c.TelephoneNumber, c.Home, c.Email, c.JobTitle, c.Department, c.Company, c.Office, c.ManagerName
                }.Select(Field)));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        private static string Field(string? value)
        {
            value ??= "";
            return value.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
