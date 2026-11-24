using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class RemoveContactCommandHandler : BaseHandler, IRequestHandler<RemoveContactCommand>
    {
        private readonly ContactRules contactRules;

        public RemoveContactCommandHandler(ContactRules contactRules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
             : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
        }

        public async Task Handle(RemoveContactCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>()
                .GetAsync(x => x.Id == request.Id && !x.IsDeleted, enableTracking: true);
            await contactRules.ContactMustExist(contact);

            // Soft delete: mobil senkron ve tuşlu telefon dosyası silinen kişiyi bu sayede öğrenir.
            contact!.IsDeleted = true;
            await unitOfWork.SaveAsync();
        }
    }
}
