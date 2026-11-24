namespace StaffSync.Application.Features.Results.ContactResults
{
    public class FilterOptionDto
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class ContactFilterOptionsResult
    {
        public List<FilterOptionDto> Departments { get; set; } = new();
        public List<FilterOptionDto> Companies { get; set; } = new();
    }
}
