using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Application.Interfaces.AutoMapper;
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
            IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(mapper, unitOfWork, httpContextAccessor)
        {
            this.userManager = userManager;
            this.authRules = authRules;
        }

        public  async Task<Unit> Handle(RevokeCommand request, CancellationToken cancellationToken)
        {
            User user = await userManager.FindByEmailAsync(request.Email); // kullanıcıyı email adresine göre buluyoruz
            await authRules.EmailAddressShouldBeValid(user); // email adresi hatalı ise hata fırlatıyoruz

            user.RefreshToken = null; // kullanıcı refresh token'ını siliyoruz
            await userManager.UpdateAsync(user); // kullanıcıyı güncelliyoruz

            return Unit.Value;
        }
    }
}
