using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.Directory;
using StaffSync.Application.Interfaces.Photos;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class RemoveContactPhotoCommandHandler : BaseHandler, IRequestHandler<RemoveContactPhotoCommand>
    {
        private readonly ContactRules contactRules;
        private readonly IContactPhotoStore photoStore;
        private readonly IDirectoryChangeNotifier directoryChangeNotifier;

        public RemoveContactPhotoCommandHandler(ContactRules contactRules, IContactPhotoStore photoStore,
            IDirectoryChangeNotifier directoryChangeNotifier, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
            this.photoStore = photoStore;
            this.directoryChangeNotifier = directoryChangeNotifier;
        }

        public async Task Handle(RemoveContactPhotoCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>()
                .GetAsync(x => x.Id == request.ContactId && !x.IsDeleted, enableTracking: true);
            await contactRules.ContactMustExist(contact);

            await photoStore.DeleteAsync(contact!.Id, cancellationToken);
            contact.ImageUrl = "";
            await unitOfWork.SaveAsync();
            directoryChangeNotifier.NotifyChanged();
        }
    }
}
