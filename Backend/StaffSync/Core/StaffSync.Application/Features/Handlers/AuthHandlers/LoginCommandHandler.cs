using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Results.AuthResults;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.Tokens;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Handlers.AuthHandlers
{
    public class LoginCommandHandler : BaseHandler, IRequestHandler<LoginCommand, LoginCommandResult>
    {
        private readonly UserManager<User> userManager;
        private readonly IConfiguration configuration;
        private readonly ITokenService tokenService;
        private readonly AuthRules authRules;
        public LoginCommandHandler(UserManager<User> userManager, IConfiguration configuration, ITokenService tokenService, AuthRules authRules,
            IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(mapper, unitOfWork, httpContextAccessor)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.tokenService = tokenService;
            this.authRules = authRules;
        }

        public async Task<LoginCommandResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            //kod açıklamaları
            User user = await userManager.FindByEmailAsync(request.Email); // kullanıcıyı email adresine göre buluyoruz
            bool checkPassword = await userManager.CheckPasswordAsync(user, request.Password); // kullanıcının şifresini kontrol ediyoruz

            await authRules.EmailOrPasswordShouldNotBeInvalid(user, checkPassword); // email veya şifre hatalı ise hata fırlatıyoruz

            IList<string> roles = await userManager.GetRolesAsync(user); // kullanıcının rollerini alıyoruz


            JwtSecurityToken token = await tokenService.CreateToken(user, roles); // token oluşturuyoruz
            string refreshToken = tokenService.GenerateRefreshToken(); // refresh token oluşturuyoruz

            _ = int.TryParse(configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays); // refresh token geçerlilik süresini alıyoruz

            user.RefreshToken = refreshToken; // kullanıcıya refresh token ekliyoruz
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays); // refresh token geçerlilik süresini ekliyoruz

            await userManager.UpdateAsync(user); // kullanıcıyı güncelliyoruz
            await userManager.UpdateSecurityStampAsync(user); // kullanıcı güvenlik damgasını güncelliyoruz

            string _token = new JwtSecurityTokenHandler().WriteToken(token); // tokeni yazıya çeviriyoruz

            await userManager.SetAuthenticationTokenAsync(user, "Default", "AccessToken", _token); // kullanıcıya token ekliyoruz

            return new()
            {
                Token = _token,
                RefreshToken = refreshToken,
                Expiration = token.ValidTo
            };
        }
    }
}
