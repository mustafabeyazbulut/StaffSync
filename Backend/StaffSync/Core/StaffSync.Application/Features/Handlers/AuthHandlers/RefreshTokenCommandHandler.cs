using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Results.AuthResults;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.Tokens;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace StaffSync.Application.Features.Handlers.AuthHandlers
{
    public class RefreshTokenCommandHandler : BaseHandler, IRequestHandler<RefreshTokenCommand, RefreshTokenCommandResult>
    {
        private readonly AuthRules authRules;
        private readonly UserManager<User> userManager;
        private readonly ITokenService tokenService;
        public RefreshTokenCommandHandler( AuthRules authRules, UserManager<User> userManager, ITokenService tokenService,
            IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(mapper, unitOfWork, httpContextAccessor)
        {
            this.authRules = authRules;
            this.userManager = userManager;
            this.tokenService = tokenService;
        }

        public async Task<RefreshTokenCommandResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            ClaimsPrincipal? principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken); // tokeni parse ediyoruz
            string email = principal.FindFirstValue(ClaimTypes.Email); // email adresini alıyoruz

            User? user = await userManager.FindByEmailAsync(email); // kullanıcıyı email adresine göre buluyoruz
            IList<string> roles = await userManager.GetRolesAsync(user); // kullanıcının rollerini alıyoruz

            await authRules.RefreshTokenShouldNotBeExpired(user.RefreshTokenExpiryTime); // refresh token süresi geçmiş ise hata fırlatıyoruz

            JwtSecurityToken newAccessToken = await tokenService.CreateToken(user, roles); // yeni token oluşturuyoruz
            string newRefreshToken = tokenService.GenerateRefreshToken(); // yeni refresh token oluşturuyoruz

            user.RefreshToken = newRefreshToken;
            await userManager.UpdateAsync(user);

            return new()
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken,
            };
        }
    }
}
