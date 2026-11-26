using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using StaffSync.Application.Bases;
using Microsoft.IdentityModel.Tokens;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Exceptions.AuthExceptions;
using StaffSync.Application.Features.Results.AuthResults;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Application.Features.Sessions;
using StaffSync.Application.Interfaces.Tokens;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration configuration;
        public RefreshTokenCommandHandler(AuthRules authRules, UserManager<User> userManager, ITokenService tokenService,
            IConfiguration configuration, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
        {
            this.authRules = authRules;
            this.userManager = userManager;
            this.tokenService = tokenService;
            this.configuration = configuration;
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

            // Anahtar bu cihazın oturumuna verilenle aynı olmalı; başka cihazların oturumları ayrıdır
            string deviceId = SessionTokens.NormalizeDeviceId(request.DeviceId);
            UserSession? session = user is null ? null : await unitOfWork.GetReadRepository<UserSession>()
                .GetAsync(x => x.UserId == user.Id && x.DeviceId == deviceId, enableTracking: true);

            await authRules.RefreshTokenShouldBeValid(session, request.RefreshToken);
            await authRules.RefreshTokenShouldNotBeExpired(session!); // oturum süresi geçmiş ise hata fırlatıyoruz

            IList<string> roles = await userManager.GetRolesAsync(user!); // kullanıcının rollerini alıyoruz

            JwtSecurityToken newAccessToken = await tokenService.CreateToken(user!, roles); // yeni token oluşturuyoruz
            string newRefreshToken = tokenService.GenerateRefreshToken(); // yeni refresh token oluşturuyoruz

            // Anahtar döner ve süre bugünden itibaren yeniden başlar (kayan süre)
            SessionTokens.Renew(session!, newRefreshToken, SessionTokens.ValidityDays(configuration));
            await unitOfWork.SaveAsync();

            return new()
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken,
            };
        }
    }
}
