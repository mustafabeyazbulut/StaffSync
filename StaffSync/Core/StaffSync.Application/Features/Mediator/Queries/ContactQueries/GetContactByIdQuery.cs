using MediatR;
using StaffSync.Application.Features.Mediator.Results.ContactResults;


namespace StaffSync.Application.Features.Mediator.Queries.ContactQueries
{
    public class GetContactByIdQuery:IRequest<GetContactByIdQueryResult>
    {
        public int ContactId { get; set; }
        public GetContactByIdQuery(int contactId)
        {
            ContactId = contactId;
        }
    }
}
