using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace StaffSync.Models
{
    public partial class ContactDto : ObservableObject
    {
        // --- Ekran durumu (sunucudan gelmez, önbelleğe yazılmaz) ---

        /// <summary>İndirilmiş fotoğrafın cihazdaki yolu; fotoğraf yoksa ya da henüz inmediyse null.</summary>
        [ObservableProperty]
        [JsonIgnore]
        public partial string? PhotoPath { get; set; }

        /// <summary>Ara düğmesinde yazan: şirket hattında kısa kod ("1001"), değilse "Ara".</summary>
        [ObservableProperty]
        [JsonIgnore]
        public partial string CallLabel { get; set; } = "Ara";

        [JsonIgnore]
        public bool HasPhotoUrl => !string.IsNullOrWhiteSpace(ImageUrl);

        // --- Sunucu alanları ---
        public int Id { get; set; }
        public string DisplayName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? Email { get; set; }
        public string? TelephoneNumber { get; set; }
        public string? Home { get; set; } // Kısa kod / dahili
        public string? Office { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Company { get; set; }
        public string? ManagerName { get; set; }

        // UI Yardımcıları
        public string Initials => Utils.DisplayText.Initials(DisplayName);

        public string Subtitle
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(JobTitle) && !string.IsNullOrWhiteSpace(Department))
                    return $"{JobTitle} · {Department}";
                return JobTitle ?? Department ?? Company ?? "";
            }
        }

        public bool HasPhone => !string.IsNullOrWhiteSpace(TelephoneNumber);
        public bool HasShortCode => !string.IsNullOrWhiteSpace(Home);
        public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    }
}
