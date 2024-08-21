using MediatR;
using StaffSync.Application.Features.Mediator.Commands.ContactCommands;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers.ContactHandlers
{
    public class RemoveContactCommandHandler : IRequestHandler<RemoveContactCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        public RemoveContactCommandHandler( IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(RemoveContactCommand request, CancellationToken cancellationToken)
        {
            var value = await _unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.Id);

            await _unitOfWork.GetWriteRepository<Contact>().HardDeleteAsync(value);
            await _unitOfWork.SaveAsync();
        }
    }
}
