namespace StaffSync.Application.Features.Results.ContactResults
{
    public class ContactChangesResult
    {
        // İstemci bir sonraki istekte since olarak bunu gönderir.
        public DateTime ServerTimeUtc { get; set; }
        public List<ContactDto> Upserted { get; set; } = new();
        public List<int> DeletedIds { get; set; } = new();
    }
}
