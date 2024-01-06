using MediatR;
using StaffSync.Application.Features.Mediator.Commands;
using StaffSync.Application.Interfaces;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers
{
    public class RemoveContactCommandHandler : IRequestHandler<RemoveContactCommand>
    {
        private readonly IRepository<Contact> _repository;
        public RemoveContactCommandHandler(IRepository<Contact> repository)
        {
            _repository = repository;
        }
        public async Task Handle(RemoveContactCommand request, CancellationToken cancellationToken)
        {
            var value = await _repository.GetByIdAsync(request.ContactId);
            await _repository.RemoveAsync(value);
        }
    }
}
