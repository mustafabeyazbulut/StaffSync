using MediatR;
using StaffSync.Application.Features.Exceptions.ContactExceptions;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Interfaces.Photos;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactPhotoQueryHandler : IRequestHandler<GetContactPhotoQuery, ContactPhoto>
    {
        private readonly IContactPhotoStore photoStore;

        public GetContactPhotoQueryHandler(IContactPhotoStore photoStore)
        {
            this.photoStore = photoStore;
        }

        public async Task<ContactPhoto> Handle(GetContactPhotoQuery request, CancellationToken cancellationToken)
            => await photoStore.GetAsync(request.ContactId, cancellationToken)
               ?? throw new ContactPhotoNotFoundException();
    }
}
