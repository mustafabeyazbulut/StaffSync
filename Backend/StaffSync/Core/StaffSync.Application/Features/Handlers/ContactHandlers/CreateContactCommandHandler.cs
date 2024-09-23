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
            //if(contacts.Any(x=>x.TelephoneNumber==request.TelephoneNumber)) throw new Exception("Contact already exists");
            await _contactRules.ContactTelephoneNumberMustNotBeSame(contacts, request.TelephoneNumber);

            Contact contact = new Contact
            {
                Company = request.Company,
                CoverImageUrl = request.CoverImageUrl,
                Department = request.Department,
                DisplayName = request.DisplayName,
                Email = request.Email,
                FirstName = request.FirstName,
                JobTitle = request.JobTitle,
                LastName = request.LastName,
                TelephoneNumber = request.TelephoneNumber,
            };
            await unitOfWork.GetWriteRepository<Contact>().AddAysnc(contact);
            var result = await unitOfWork.SaveAsync();
            return Unit.Value;
        }
    }
}
