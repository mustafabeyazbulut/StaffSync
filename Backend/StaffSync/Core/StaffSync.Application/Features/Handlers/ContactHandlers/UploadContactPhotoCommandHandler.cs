using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Photos;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.Directory;
using StaffSync.Application.Interfaces.Photos;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class UploadContactPhotoCommandHandler : BaseHandler, IRequestHandler<UploadContactPhotoCommand>
    {
        private readonly ContactRules contactRules;
        private readonly IContactPhotoStore photoStore;
        private readonly IDirectoryChangeNotifier directoryChangeNotifier;

        public UploadContactPhotoCommandHandler(ContactRules contactRules, IContactPhotoStore photoStore,
            IDirectoryChangeNotifier directoryChangeNotifier, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
            this.photoStore = photoStore;
            this.directoryChangeNotifier = directoryChangeNotifier;
        }

        public async Task Handle(UploadContactPhotoCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>()
                .GetAsync(x => x.Id == request.ContactId && !x.IsDeleted, enableTracking: true);
            await contactRules.ContactMustExist(contact);

            // Validator türü doğruladı; burada yalnızca hangisi olduğunu alıyoruz
            string contentType = ImageFormats.DetectContentType(request.Content)!;
            await photoStore.SaveAsync(contact!.Id, request.Content, contentType, cancellationToken);

            // Adres sürüm içerir: fotoğraf değişince adres de değişir, istemciler önbellekteki eski
            // fotoğrafı kullanmaz. Kişi güncellendiği için (UpdatedDate) mobil senkron da değişikliği alır.
            contact.ImageUrl = ContactPhotoUrls.For(contact.Id);
            await unitOfWork.SaveAsync();
            directoryChangeNotifier.NotifyChanged();
        }
    }
}
