using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.DirectoryExport;
using StaffSync.Application.Features.Queries.DirectoryQueries;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.DirectoryHandlers
{
    public class GetDirectoryExportQueryHandler : BaseHandler, IRequestHandler<GetDirectoryExportQuery, DirectoryExportFile>
    {
        public GetDirectoryExportQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<DirectoryExportFile> Handle(GetDirectoryExportQuery request, CancellationToken cancellationToken)
        {
            List<Contact> contacts = await unitOfWork.GetReadRepository<Contact>().AsQueryable()
                .Where(c => !c.IsDeleted).OrderBy(c => c.DisplayName).ToListAsync(cancellationToken);

            return request.Format.Equals("csv", StringComparison.OrdinalIgnoreCase)
                ? DirectoryExportFiles.Csv(contacts)
                : DirectoryExportFiles.Vcf(contacts);
        }
    }
}
