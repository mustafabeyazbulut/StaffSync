using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class CreateContactCommandHandler : BaseHandler, IRequestHandler<CreateContactCommand, Unit>
    {
        private readonly ContactRules _contactRules;

        public CreateContactCommandHandler(ContactRules contactRules, IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(mapper, unitOfWork, httpContextAccessor)
        {
            _contactRules = contactRules;
        }
        public async Task<Unit> Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            IList<Contact> contacts = await unitOfWork.GetReadRepository<Contact>().GetAllAsync();
            await _contactRules.ContactTelephoneNumberMustNotBeSame(contacts, request.TelephoneNumber);

            Contact contact = new Contact
            {
                DisplayName = request.DisplayName,
                ImageUrl=request.ImageUrl,
                Email = request.Email,
                TelephoneNumber = request.TelephoneNumber,
                Home=request.Home,
                Office = request.Office,
                JobTitle = request.JobTitle,
                Department = request.Department,
                Company = request.Company,
                ManagerName = request.ManagerName
            };
            await unitOfWork.GetWriteRepository<Contact>().AddAysnc(contact);
            var result = await unitOfWork.SaveAsync();
            return Unit.Value;
        }
    }
}
