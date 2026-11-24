using MediatR;
using StaffSync.Application.Features.DirectoryExport;

namespace StaffSync.Application.Features.Queries.DirectoryQueries
{
    public class GetDirectoryExportQuery : IRequest<DirectoryExportFile>
    {
        public string Format { get; set; } = "vcf"; // "vcf" | "csv"
    }
}
