using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.Directory;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class UpdateContactCommandHandler : BaseHandler, IRequestHandler<UpdateContactCommand>
    {
        private readonly ContactRules contactRules;
        private readonly IDirectoryChangeNotifier directoryChangeNotifier;

        public UpdateContactCommandHandler(ContactRules contactRules, IDirectoryChangeNotifier directoryChangeNotifier,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
            this.directoryChangeNotifier = directoryChangeNotifier;
        }

        public async Task Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>()
                .GetAsync(x => x.Id == request.Id && !x.IsDeleted, enableTracking: true);
            await contactRules.ContactMustExist(contact);

            request.ApplyTo(contact!);
            await unitOfWork.SaveAsync();
            directoryChangeNotifier.NotifyChanged();
        }
    }
}
