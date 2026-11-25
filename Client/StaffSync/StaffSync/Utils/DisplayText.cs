using System.Globalization;
using StaffSync.Models;

namespace StaffSync.Utils
{
    /// <summary>
    /// Ekranlarda ortak kullanılan metin biçimleri. Aynı bilgi (baş harfler, son senkron zamanı,
    /// oturumdaki kullanıcının adı) her ekranda aynı görünsün diye tek yerde tutulur.
    /// </summary>
    public static class DisplayText
    {
        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>Telefon rehberinde kısa kod numarasının etiketi.</summary>
        public const string ShortCodeLabel = "Kısa kod";

        /// <summary>"Ayşe Nur Yılmaz" → "AY", "mbeyazbulut" → "M", boş → "?"</summary>
        public static string Initials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = parts.Length == 1
                ? parts[0][..1]
                : $"{parts[0][0]}{parts[^1][0]}";
            return initials.ToUpper(Turkish);
        }

        /// <summary>"Bugün 14:05", "Dün 09:30", "21.11.2026 16:00"; hiç senkron yoksa null.</summary>
        public static string? LastSync(DateTime? time)
        {
            if (!time.HasValue)
                return null;

            var local = time.Value.ToLocalTime();
            var today = DateTime.Today;
            if (local.Date == today)
                return $"Bugün {local:HH:mm}";
            if (local.Date == today.AddDays(-1))
                return $"Dün {local:HH:mm}";
            return local.ToString("dd.MM.yyyy HH:mm", Turkish);
        }

        /// <summary>
        /// Oturumdaki kullanıcının gösterilecek adı: rehberde e-postasıyla eşleşen kişi varsa onun adı,
        /// yoksa hesaptaki ad, o da yoksa e-postanın @ öncesi.
        /// </summary>
        public static string UserDisplayName(UserInfo user, IEnumerable<ContactDto> contacts)
        {
            var match = FindSelf(user, contacts);
            if (!string.IsNullOrWhiteSpace(match?.DisplayName))
                return match.DisplayName;
            if (!string.IsNullOrWhiteSpace(user.Name))
                return user.Name;
            return user.Email.Split('@')[0];
        }

        /// <summary>Rehberde oturumdaki kullanıcıya ait kayıt (e-posta eşleşmesi).</summary>
        public static ContactDto? FindSelf(UserInfo user, IEnumerable<ContactDto> contacts)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                return null;
            var email = user.Email.Trim();
            return contacts.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.Email) &&
                string.Equals(c.Email.Trim(), email, StringComparison.OrdinalIgnoreCase));
        }
    }
}
