using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    // Tuşlu telefonların okuduğu vCard 2.1. ASCII değerler düz, diğerleri UTF-8 + quoted-printable yazılır.
    public static class VCardWriter
    {
        public static string Write(IEnumerable<Contact> contacts)
        {
            var sb = new StringBuilder();
            foreach (Contact c in contacts) WriteOne(sb, c);
            return sb.ToString();
        }

        private static void WriteOne(StringBuilder sb, Contact c)
        {
            var (first, last) = SplitName(c.DisplayName ?? "");

            sb.Append("BEGIN:VCARD\r\n").Append("VERSION:2.1\r\n");
            Prop(sb, "N", $"{Escape(last)};{Escape(first)}", alwaysWrite: true);
            Prop(sb, "FN", c.DisplayName);
            Prop(sb, "TEL;CELL", c.TelephoneNumber);
            Prop(sb, "TEL;WORK", c.Home);
            Prop(sb, "EMAIL;INTERNET", c.Email);
            if (!string.IsNullOrWhiteSpace(c.Company) || !string.IsNullOrWhiteSpace(c.Department))
                Prop(sb, "ORG", $"{Escape(c.Company ?? "")};{Escape(c.Department ?? "")}");
            Prop(sb, "TITLE", c.JobTitle);
            sb.Append("END:VCARD\r\n");
        }

        private static void Prop(StringBuilder sb, string name, string? value, bool alwaysWrite = false)
        {
            if (string.IsNullOrWhiteSpace(value) && !alwaysWrite) return;
            value ??= "";

            if (value.All(ch => ch < 128))
            {
                sb.Append(name).Append(':').Append(value).Append("\r\n");
                return;
            }

            string prefix = name + ";CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:";
            sb.Append(prefix).Append(QuotedPrintable.Encode(value, prefix.Length)).Append("\r\n");
        }

        // "Ali Rıza Kaya" → ("Ali Rıza", "Kaya"); tek kelime → (kelime, "").
        private static (string First, string Last) SplitName(string displayName)
        {
            string[] parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => ("", ""),
                1 => (parts[0], ""),
                _ => (string.Join(' ', parts[..^1]), parts[^1])
            };
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace(";", "\\;");
    }
}
