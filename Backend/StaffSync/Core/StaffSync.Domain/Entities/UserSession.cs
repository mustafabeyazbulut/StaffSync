using StaffSync.Domain.Common;

namespace StaffSync.Domain.Entities
{
    /// <summary>
    /// Bir kullanıcının bir cihazdaki oturumu. Her cihaz kendi yenileme anahtarını taşır; bir cihazdan
    /// giriş yapmak diğer cihazların oturumunu düşürmez. Anahtarın kendisi değil SHA-256 özeti saklanır.
    /// </summary>
    public class UserSession : EntityBase, IEntityBase
    {
        public Guid UserId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string RefreshTokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime LastUsedAtUtc { get; set; }
    }
}
