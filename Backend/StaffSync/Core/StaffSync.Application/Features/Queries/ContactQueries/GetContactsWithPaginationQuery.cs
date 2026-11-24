using MediatR;
using StaffSync.Application.Features.Results.Common;
using StaffSync.Application.Features.Results.ContactResults;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactsWithPaginationQuery : IRequest<PagedResult<ContactDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 30;
        public string? Search { get; set; }
        public List<string>? Departments { get; set; }
        public string? Company { get; set; }
        public ContactSortBy SortBy { get; set; } = ContactSortBy.Name;
    }
}
