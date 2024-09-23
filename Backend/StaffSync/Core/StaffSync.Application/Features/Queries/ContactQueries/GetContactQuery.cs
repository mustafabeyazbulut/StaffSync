using MediatR;
using StaffSync.Application.Features.Results.ContactResults;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactQuery : IRequest<List<GetContactQueryResult>>
    {
    }
}
