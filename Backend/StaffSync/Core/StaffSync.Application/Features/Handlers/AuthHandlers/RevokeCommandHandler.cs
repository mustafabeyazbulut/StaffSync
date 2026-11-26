using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Handlers.AuthHandlers
{
    public class RevokeCommandHandler : BaseHandler, IRequestHandler<RevokeCommand, Unit>
    {
        private readonly UserManager<User> userManager;
        private readonly AuthRules authRules;
        public RevokeCommandHandler(UserManager<User> userManager, AuthRules authRules,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
        {
            this.userManager = userManager;
            this.authRules = authRules;
        }

        public  async Task<Unit> Handle(RevokeCommand request, CancellationToken cancellationToken)
        {
            User user = await userManager.FindByEmailAsync(request.Email); // kullanıcıyı email adresine göre buluyoruz
            await authRules.EmailAddressShouldBeValid(user); // email adresi hatalı ise hata fırlatıyoruz

            // Kullanıcının tüm cihazlardaki oturumlarını kapatıyoruz
            IWriteRepository<UserSession> sessionWriter = unitOfWork.GetWriteRepository<UserSession>();
            foreach (UserSession session in await unitOfWork.GetReadRepository<UserSession>().GetAllAsync(x => x.UserId == user.Id, enableTracking: true))
                await sessionWriter.HardDeleteAsync(session);
            await unitOfWork.SaveAsync();

            return Unit.Value;
        }
    }
}
