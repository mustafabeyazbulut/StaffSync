using MediatR;
using StaffSync.Application.Features.Results.ContactResults;


namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactByIdQuery : IRequest<ContactDto>
    {
        public int ContactId { get; set; }
        public GetContactByIdQuery(int contactId)
        {
            ContactId = contactId;
        }
    }
}
