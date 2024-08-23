using MediatR;
using StaffSync.Application.Features.Mediator.Commands.ContactCommands;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers.ContactHandlers
{
    public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ContactRules _contactRules;

        public CreateContactCommandHandler(IUnitOfWork unitOfWork, ContactRules contactRules)
        {
            _unitOfWork = unitOfWork;
            this._contactRules = contactRules;
        }
        public async Task<Unit> Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            IList<Contact> contacts = await _unitOfWork.GetReadRepository<Contact>().GetAllAsync();
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
            await _unitOfWork.GetWriteRepository<Contact>().AddAysnc(contact);
            var result = await _unitOfWork.SaveAsync();
            return Unit.Value;
        }
    }
}
