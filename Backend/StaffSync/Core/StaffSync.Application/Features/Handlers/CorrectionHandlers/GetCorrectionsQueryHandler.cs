using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.CorrectionQueries;
using StaffSync.Application.Features.Results.CorrectionResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.CorrectionHandlers
{
    public class GetCorrectionsQueryHandler : BaseHandler, IRequestHandler<GetCorrectionsQuery, List<CorrectionDto>>
    {
        public GetCorrectionsQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<List<CorrectionDto>> Handle(GetCorrectionsQuery request, CancellationToken cancellationToken)
        {
            IList<ContactCorrection> list = await unitOfWork.GetReadRepository<ContactCorrection>().GetAllAsync(
                predicate: request.Status is null ? null : x => x.Status == request.Status,
                include: q => q.Include(x => x.Items).Include(x => x.Contact!),
                orderby: q => q.OrderByDescending(x => x.CreatedDate));

            return list.Select(c => c.ToDto()).ToList();
        }
    }
}
