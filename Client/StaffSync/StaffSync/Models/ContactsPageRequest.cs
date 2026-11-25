namespace StaffSync.Models
{
    public class ContactsPageRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Search { get; set; }
        public List<string>? Departments { get; set; }
        public string? Company { get; set; }
        public string SortBy { get; set; } = "Name";
    }
}
