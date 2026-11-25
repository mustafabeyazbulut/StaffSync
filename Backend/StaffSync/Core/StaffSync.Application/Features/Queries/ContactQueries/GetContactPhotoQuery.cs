using MediatR;
using StaffSync.Application.Interfaces.Photos;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactPhotoQuery : IRequest<ContactPhoto>
    {
        public GetContactPhotoQuery(int contactId) => ContactId = contactId;
        public int ContactId { get; }
    }
}
