using StaffSync.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Interfaces.Tokens
{
    public interface ITokenService
    {
        Task<JwtSecurityToken> CreateToken(User user, IList<string> roles);
        string GenerateRefreshToken();

        // Bu fonksiyon token'ı parse edip içindeki bilgileri alıp bir principal oluşturuyor. Yani token'ı parse edip içindeki bilgileri almak için kullanılıyor.
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token); 
        
    }
}
