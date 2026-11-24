using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.Directory;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class RemoveContactCommandHandler : BaseHandler, IRequestHandler<RemoveContactCommand>
    {
        private readonly ContactRules contactRules;
        private readonly IDirectoryChangeNotifier directoryChangeNotifier;

        public RemoveContactCommandHandler(ContactRules contactRules, IDirectoryChangeNotifier directoryChangeNotifier,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
             : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
            this.directoryChangeNotifier = directoryChangeNotifier;
        }

        public async Task Handle(RemoveContactCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>()
                .GetAsync(x => x.Id == request.Id && !x.IsDeleted, enableTracking: true);
            await contactRules.ContactMustExist(contact);

            // Soft delete: mobil senkron ve tuşlu telefon dosyası silinen kişiyi bu sayede öğrenir.
            contact!.IsDeleted = true;
            await unitOfWork.SaveAsync();
            directoryChangeNotifier.NotifyChanged();
        }
    }
}
