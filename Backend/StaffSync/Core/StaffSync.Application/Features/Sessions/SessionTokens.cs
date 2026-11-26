using StaffSync.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace StaffSync.Application.Features.Sessions
{
    /// <summary>Cihaz oturumlarında kullanılan yardımcılar: cihaz kimliği ve yenileme anahtarı özeti.</summary>
    public static class SessionTokens
    {
        public const int DeviceIdMaxLength = 64;

        /// <summary>Cihaz kimliği göndermeyen (eski) istemciler tek bir ortak oturum kullanır.</summary>
        public const string DefaultDeviceId = "default";

        public static string NormalizeDeviceId(string? deviceId) =>
            string.IsNullOrWhiteSpace(deviceId) ? DefaultDeviceId : deviceId.Trim();

        /// <summary>Yenileme anahtarı DB'de açık saklanmaz; SHA-256 özeti (64 karakter hex) saklanır.</summary>
        public static string Hash(string refreshToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        /// <summary>
        /// Oturuma yeni anahtarı yazar ve süresini bugünden itibaren yeniden başlatır (kayan süre):
        /// uygulamayı kullanan kişi süre dolmadıkça bir daha şifre girmez.
        /// </summary>
        public static void Renew(UserSession session, string refreshToken, int validityDays)
        {
            DateTime now = DateTime.UtcNow;
            session.RefreshTokenHash = Hash(refreshToken);
            session.ExpiresAtUtc = now.AddDays(validityDays);
            session.LastUsedAtUtc = now;
        }

        public static int ValidityDays(IConfiguration configuration) =>
            int.TryParse(configuration["JWT:RefreshTokenValidityInDays"], out int days) && days > 0 ? days : 60;
    }
}
