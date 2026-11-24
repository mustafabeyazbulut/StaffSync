namespace StaffSync.Application.Features.Results.ContactResults
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
    }
}
