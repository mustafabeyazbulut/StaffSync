using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.Common;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactsWithPaginationQueryHandler : BaseHandler, IRequestHandler<GetContactsWithPaginationQuery, PagedResult<ContactDto>>
    {
        public GetContactsWithPaginationQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<PagedResult<ContactDto>> Handle(GetContactsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Contact> filtered = unitOfWork.GetReadRepository<Contact>().AsQueryable()
                .ApplyFilters(request.Search, request.Departments, request.Company);

            int total = await filtered.CountAsync(cancellationToken);
            List<ContactDto> items = await filtered
                .ApplySort(request.SortBy)
                .Page(request.PageNumber, request.PageSize)
                .Select(ContactMappings.AsDto)
                .ToListAsync(cancellationToken);

            return new PagedResult<ContactDto>(items, total, request.PageNumber, request.PageSize);
        }
    }
}
