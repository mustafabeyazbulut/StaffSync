using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class RemoveContactCommandHandler : BaseHandler, IRequestHandler<RemoveContactCommand>
    {
        public RemoveContactCommandHandler(IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
             : base(mapper, unitOfWork, httpContextAccessor)
        {
        }
        public async Task Handle(RemoveContactCommand request, CancellationToken cancellationToken)
        {
            var value = await unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.Id);

            await unitOfWork.GetWriteRepository<Contact>().HardDeleteAsync(value);
            await unitOfWork.SaveAsync();
        }
    }
}
