namespace StaffSync.Models
{
    public class ContactChangesResult
    {
        public DateTime ServerTimeUtc { get; set; }
        public List<ContactDto> Upserted { get; set; } = new();
        public List<int> DeletedIds { get; set; } = new();
    }
}
