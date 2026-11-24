using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    // vCard 2.1'in istediği quoted-printable kodlaması (UTF-8 baytları üzerinden).
    public static class QuotedPrintable
    {
        private const int MaxLine = 76;

        // firstLineOffset: değerin önündeki "AD;PARAMETRELER:" kısmının uzunluğu (ilk satır sınırına dahil).
        public static string Encode(string value, int firstLineOffset)
        {
            var sb = new StringBuilder();
            int lineLength = firstLineOffset;

            foreach (byte b in Encoding.UTF8.GetBytes(value))
            {
                string token = b is >= 33 and <= 126 && b != (byte)'=' ? ((char)b).ToString() : $"={b:X2}";

                // Yumuşak satır sonu "=" de satıra sayılır; "=XX" asla bölünmez.
                if (lineLength + token.Length > MaxLine - 1)
                {
                    sb.Append("=\r\n");
                    lineLength = 0;
                }

                sb.Append(token);
                lineLength += token.Length;
            }

            return sb.ToString();
        }
    }
}
