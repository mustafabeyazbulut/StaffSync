using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;
using System.Linq.Expressions;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactsWithPaginationQueryHandler : BaseHandler, IRequestHandler<GetContactsWithPaginationQuery, List<GetContactsWithPaginationQueryResult>>
    {
        public GetContactsWithPaginationQueryHandler(
            IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(mapper, unitOfWork, httpContextAccessor)
        {
        }

        public async Task<List<GetContactsWithPaginationQueryResult>> Handle(GetContactsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            // Dinamik filtreleme oluşturma
            Expression<Func<Contact, bool>> filter = x =>
                (!request.ContactId.HasValue || x.Id == request.ContactId.Value) && // Contact ID filtresi
                (string.IsNullOrEmpty(request.DisplayName) || x.DisplayName.Contains(request.DisplayName)) &&
                (string.IsNullOrEmpty(request.Email) || x.Email.Contains(request.Email)) &&
                (string.IsNullOrEmpty(request.Department) || x.Department.Contains(request.Department)) &&
                (string.IsNullOrEmpty(request.Company) || x.Company.Contains(request.Company));
            var contacts = await unitOfWork.GetReadRepository<Contact>()
                .GetAllByPagingAsync(filter,
                                     null, // İlişkili nesneler dahil edilmedi
                                     null, // Varsayılan sıralama
                                     enableTracking: false, // Takip edilmesin
                                     currentPage: request.PageNumber,
                                     pageSize: request.PageSize);
            var map = mapper.Map<GetContactsWithPaginationQueryResult, Contact>(contacts);

            return map.ToList();
        }
    }
}
