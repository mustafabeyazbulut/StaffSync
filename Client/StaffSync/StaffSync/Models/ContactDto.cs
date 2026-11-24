namespace StaffSync.Models
{
    public class ContactDto
    {
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
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DisplayName))
                    return "?";
                var parts = DisplayName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                    return parts[0][0].ToString().ToUpperInvariant();
                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }

        public string Subtitle
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(JobTitle) && !string.IsNullOrWhiteSpace(Department))
                    return $"{JobTitle} • {Department}";
                return JobTitle ?? Department ?? Company ?? "";
            }
        }

        public bool HasPhone => !string.IsNullOrWhiteSpace(TelephoneNumber);
        public bool HasShortCode => !string.IsNullOrWhiteSpace(Home);
        public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    }
}
