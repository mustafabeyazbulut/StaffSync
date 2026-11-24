using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class CreateContactCommandHandler : BaseHandler, IRequestHandler<CreateContactCommand, Unit>
    {
        private readonly ContactRules _contactRules;

        public CreateContactCommandHandler(ContactRules contactRules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(unitOfWork, httpContextAccessor)
        {
            _contactRules = contactRules;
        }

        public async Task<Unit> Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            IList<Contact> sameNumber = await unitOfWork.GetReadRepository<Contact>()
                .GetAllAsync(x => !x.IsDeleted && x.TelephoneNumber == request.TelephoneNumber);
            await _contactRules.ContactTelephoneNumberMustNotBeSame(sameNumber, request.TelephoneNumber);

            await unitOfWork.GetWriteRepository<Contact>().AddAysnc(request.ToEntity());
            await unitOfWork.SaveAsync();
            return Unit.Value;
        }
    }
}
