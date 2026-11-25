using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using StaffSync.Application.Bases;
using Microsoft.IdentityModel.Tokens;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Exceptions.AuthExceptions;
using StaffSync.Application.Features.Results.AuthResults;
using StaffSync.Application.Features.Rules.AuthRules;
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
        public RefreshTokenCommandHandler(AuthRules authRules, UserManager<User> userManager, ITokenService tokenService,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
        {
            this.authRules = authRules;
            this.userManager = userManager;
            this.tokenService = tokenService;
        }

        public async Task<RefreshTokenCommandResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            ClaimsPrincipal? principal;
            try
            {
                principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken); // imzası doğrulanmış, süresi dolmuş olabilen token
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
            {
                // Bozuk/sahte token 500 değil 400 dönsün; istemci oturumu kapatır
                throw new RefreshTokenShouldBeValidException();
            }

            string? email = principal?.FindFirstValue(ClaimTypes.Email);
            User? user = string.IsNullOrEmpty(email) ? null : await userManager.FindByEmailAsync(email);

            await authRules.RefreshTokenShouldBeValid(user, request.RefreshToken); // refresh token bu kullanıcıya verilenle aynı olmalı
            await authRules.RefreshTokenShouldNotBeExpired(user!.RefreshTokenExpiryTime); // refresh token süresi geçmiş ise hata fırlatıyoruz

            IList<string> roles = await userManager.GetRolesAsync(user); // kullanıcının rollerini alıyoruz

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
