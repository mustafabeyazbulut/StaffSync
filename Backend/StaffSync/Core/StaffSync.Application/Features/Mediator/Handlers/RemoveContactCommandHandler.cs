using MediatR;
using StaffSync.Application.Features.Mediator.Commands;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers
{
    public class RemoveContactCommandHandler : IRequestHandler<RemoveContactCommand>
    {
        private readonly IRepository<Contact> _repository;
        private readonly IUnitOfWork _unitOfWork;
        public RemoveContactCommandHandler(IRepository<Contact> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(RemoveContactCommand request, CancellationToken cancellationToken)
        {
            //var value = await _repository.GetByIdAsync(request.Id);
            //await _repository.RemoveAsync(value);
            var value = await _unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.Id);

            await _unitOfWork.GetWriteRepository<Contact>().HardDeleteAsync(value);
            await _unitOfWork.SaveAsync();
        }
    }
}
