using MediatR;
using StaffSync.Application.Features.Mediator.Commands.ContactCommands;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers.ContactHandlers
{
    public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper mapper;

        public UpdateContactCommandHandler( IMapper mapper, IUnitOfWork unitOfWork)
        {
            this.mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {

            var value = await _unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.Id && !x.IsDeleted);

            var map = mapper.Map<Contact, UpdateContactCommand>(request);

            await _unitOfWork.GetWriteRepository<Contact>().UpdateAsync(map);
            await _unitOfWork.SaveAsync();
        }
    }
}
