using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactQueryHandler : BaseHandler, IRequestHandler<GetContactQuery, List<ContactDto>>
    {
        public GetContactQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(unitOfWork, httpContextAccessor) { }

        public async Task<List<ContactDto>> Handle(GetContactQuery request, CancellationToken cancellationToken)
        {
            return await unitOfWork.GetReadRepository<Contact>().AsQueryable()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayName).ThenBy(c => c.Id)
                .Select(ContactMappings.AsDto)
                .ToListAsync(cancellationToken);
        }
    }
}
