using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StaffSync.Application.Interfaces.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using System.Text;
using Microsoft.IdentityModel.Tokens;
using StaffSync.Domain.Entities;
using System.Security.Cryptography;

namespace StaffSync.Infrastructure.Tokens
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<User> userManager;
        private readonly TokenSettings tokenSettings;
        public TokenService(IOptions<TokenSettings> options, UserManager<User> userManager)
        {
            tokenSettings = options.Value;
            this.userManager = userManager;
        }

        public async Task<JwtSecurityToken> CreateToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            }; // bir kullanıcının adı, soyadı, email adresi, telefon numarası gibi bilgileri bir claims nesnesinde tutulabilir.

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            } // bir kullanıcının birden fazla rolü olabilir. Bu rolleri claims nesnesine ekleyerek bir kullanıcının rollerini tutabiliriz.

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.Secret)); // token'ı oluşturmak Secreti kullanarak bir key oluşturuyoruz.

            var token = new JwtSecurityToken(
                issuer: tokenSettings.Issuer,
                audience: tokenSettings.Audience,
                expires: DateTime.Now.AddMinutes(tokenSettings.TokenValidityInMunitues),
                claims: claims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                );

            await userManager.AddClaimsAsync(user, claims);

            return token;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        { // bu fonksiyon token'ı parse edip içindeki bilgileri alıp bir principal oluşturuyor. Yani token'ı parse edip içindeki bilgileri almak için kullanılıyor.
          // principal: bir kullanıcıya ait bilgileri içeren bir nesnedir. Kullanıcıya ait bilgileri içerir.
            TokenValidationParameters tokenValidationParamaters = new()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.Secret)),
                ValidateLifetime = false
            };

            JwtSecurityTokenHandler tokenHandler = new();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParamaters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg
                .Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token bulunamadı.");

            return principal;
        }
    }
}
