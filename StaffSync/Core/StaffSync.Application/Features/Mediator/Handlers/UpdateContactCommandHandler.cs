using MediatR;
using StaffSync.Application.Features.Mediator.Commands;
using StaffSync.Application.Interfaces;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers
{
    public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand>
    {
        private readonly IRepository<Contact> _repository;

        public UpdateContactCommandHandler(IRepository<Contact> repository)
        {
            _repository = repository;
        }
        public async Task Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {
            var value = await _repository.GetByIdAsync(request.ContactId);
            value.CoverImageUrl = request.CoverImageUrl;
            value.FirstName = request.FirstName;
            value.LastName = request.LastName;
            value.DisplayName = request.DisplayName;
            value.Email = request.Email;
            value.TelephoneNumber = request.TelephoneNumber;
            value.JobTitle = request.JobTitle;
            value.Department = request.Department;
            value.Company = request.Company;
            await _repository.UpdateAsync(value);
        }
    }
}
