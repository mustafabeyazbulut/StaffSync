using MediatR;
using StaffSync.Application.Features.Results.ContactResults;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactChangesQuery : IRequest<ContactChangesResult>
    {
        public DateTime? Since { get; set; }
    }
}
