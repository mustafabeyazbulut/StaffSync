using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.AuthHandlers
{
    public class RevokeAllCommandHandler : BaseHandler, IRequestHandler<RevokeAllCommand, Unit>
    {
        public RevokeAllCommandHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<Unit> Handle(RevokeAllCommand request, CancellationToken cancellationToken)
        {
            // Herkesin tüm cihazlardaki oturumlarını kapatıyoruz
            IWriteRepository<UserSession> sessionWriter = unitOfWork.GetWriteRepository<UserSession>();
            foreach (UserSession session in await unitOfWork.GetReadRepository<UserSession>().GetAllAsync(enableTracking: true))
                await sessionWriter.HardDeleteAsync(session);
            await unitOfWork.SaveAsync();
            return Unit.Value;
        }
    }
}
