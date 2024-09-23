using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class UpdateContactCommandHandler : BaseHandler, IRequestHandler<UpdateContactCommand>
    {
        public UpdateContactCommandHandler(IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(mapper, unitOfWork, httpContextAccessor)
        {
        }
        public async Task Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {

            var value = await unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.Id && !x.IsDeleted);

            var map = mapper.Map<Contact, UpdateContactCommand>(request);

            await unitOfWork.GetWriteRepository<Contact>().UpdateAsync(map);
            await unitOfWork.SaveAsync();
        }
    }
}
