using StaffSync.Domain.Common;

namespace StaffSync.Domain.Entities
{
    public class ContactCorrection : EntityBase, IEntityBase
    {
        public int ContactId { get; set; }
        public Contact? Contact { get; set; }
        public Guid ReportedByUserId { get; set; }
        public string? Note { get; set; }
        public CorrectionStatus Status { get; set; } = CorrectionStatus.New;
        public List<ContactCorrectionItem> Items { get; set; } = new();
    }
}
