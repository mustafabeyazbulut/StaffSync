using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;
using System.Linq.Expressions;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactsWithPaginationQueryHandler : BaseHandler, IRequestHandler<GetContactsWithPaginationQuery, List<ContactDto>>
    {
        public GetContactsWithPaginationQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<List<ContactDto>> Handle(GetContactsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            Expression<Func<Contact, bool>> filter = x =>
                (!request.ContactId.HasValue || x.Id == request.ContactId.Value) &&
                (string.IsNullOrEmpty(request.DisplayName) || x.DisplayName.Contains(request.DisplayName)) &&
                (string.IsNullOrEmpty(request.Email) || x.Email.Contains(request.Email)) &&
                (string.IsNullOrEmpty(request.Department) || x.Department.Contains(request.Department)) &&
                (string.IsNullOrEmpty(request.Company) || x.Company.Contains(request.Company));
            var contacts = await unitOfWork.GetReadRepository<Contact>()
                .GetAllByPagingAsync(filter,
                                     null,
                                     null,
                                     enableTracking: false,
                                     currentPage: request.PageNumber,
                                     pageSize: request.PageSize);

            return contacts.Select(c => c.ToDto()).ToList();
        }
    }
}
