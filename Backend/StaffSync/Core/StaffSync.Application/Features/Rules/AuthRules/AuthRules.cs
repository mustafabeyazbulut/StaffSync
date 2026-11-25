using StaffSync.Application.Bases;
using StaffSync.Application.Features.Exceptions.AuthExceptions;
using StaffSync.Domain.Entities;
using System.Security.Cryptography;
using System.Text;


namespace StaffSync.Application.Features.Rules.AuthRules
{
    public class AuthRules : BaseRules
    {
        public Task UserShouldNotBeExist(User? user)
        {
            if (user is not null) throw new UserAlreadyExistException();
            return Task.CompletedTask;
        }
        public Task EmailOrPasswordShouldNotBeInvalid(User? user, bool checkPassword)
        {
            if (user is null || !checkPassword) throw new EmailOrPasswordShouldNotBeInvalidException();
            return Task.CompletedTask;
        }
        public Task RefreshTokenShouldNotBeExpired(DateTime? expiryDate)
        {
            if (expiryDate <= DateTime.Now) throw new RefreshTokenShouldNotBeExpiredException();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gönderilen refresh token, kullanıcıya en son verilenle birebir aynı olmalı. Aksi halde süresi
        /// dolmuş bir access token'ı ele geçiren herkes yeni oturum alabilirdi.
        /// </summary>
        public Task RefreshTokenShouldBeValid(User? user, string? refreshToken)
        {
            if (user is null
                || string.IsNullOrEmpty(user.RefreshToken)
                || string.IsNullOrEmpty(refreshToken)
                || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(user.RefreshToken),
                    Encoding.UTF8.GetBytes(refreshToken)))
                throw new RefreshTokenShouldBeValidException();
            return Task.CompletedTask;
        }

        public Task EmailAddressShouldBeValid(User? user)
        {
            if (user is null) throw new EmailAddressShouldBeValidException();
            return Task.CompletedTask;
        }
    }
}
