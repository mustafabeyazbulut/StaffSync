using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    public record DirectoryExportFile(string FileName, string ContentType, byte[] Content);

    public static class DirectoryExportFiles
    {
        public const string VcfName = "rehber.vcf";
        public const string CsvName = "rehber.csv";

        public static DirectoryExportFile Vcf(IEnumerable<Contact> contacts) =>
            new(VcfName, "text/x-vcard", Encoding.UTF8.GetBytes(VCardWriter.Write(contacts)));

        public static DirectoryExportFile Csv(IEnumerable<Contact> contacts) =>
            new(CsvName, "text/csv", CsvDirectoryWriter.Write(contacts));
    }
}
