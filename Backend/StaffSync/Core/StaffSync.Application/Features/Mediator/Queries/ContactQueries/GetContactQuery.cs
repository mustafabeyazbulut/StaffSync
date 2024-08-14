using MediatR;
using StaffSync.Application.Features.Mediator.Results.ContactResults;

namespace StaffSync.Application.Features.Mediator.Queries.ContactQueries
{
    public class GetContactQuery:IRequest<List<GetContactQueryResult>>
    {
    }
}
