using StaffSync.Application.Bases;
using StaffSync.Application.Features.Exceptions.AuthExceptions;
using StaffSync.Application.Features.Sessions;
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
        public Task RefreshTokenShouldNotBeExpired(UserSession session)
        {
            if (session.ExpiresAtUtc <= DateTime.UtcNow) throw new RefreshTokenShouldNotBeExpiredException();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gönderilen refresh token, bu cihazın oturumuna en son verilenle birebir aynı olmalı. Aksi halde
        /// süresi dolmuş bir access token'ı ele geçiren herkes yeni oturum alabilirdi.
        /// </summary>
        public Task RefreshTokenShouldBeValid(UserSession? session, string? refreshToken)
        {
            if (session is null
                || string.IsNullOrEmpty(session.RefreshTokenHash)
                || string.IsNullOrEmpty(refreshToken)
                || !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(session.RefreshTokenHash),
                    Encoding.ASCII.GetBytes(SessionTokens.Hash(refreshToken))))
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
