using MediatR;
using StaffSync.Application.Features.Mediator.Commands;
using StaffSync.Application.Interfaces;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers
{
    public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand>
    {
        private readonly IRepository<Contact> _repository;

        public CreateContactCommandHandler(IRepository<Contact> repository)
        {
            _repository = repository;
        }
        public async Task Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            await _repository.CreateAsync(new Contact
            {
                Company = request.Company,
                CoverImageUrl = request.CoverImageUrl,
                Department=request.Department,
                DisplayName = request.DisplayName,
                Email = request.Email,
                FirstName = request.FirstName,
                JobTitle = request.JobTitle,
                LastName = request.LastName,
                TelephoneNumber = request.TelephoneNumber,
            });
        }
    }
}
