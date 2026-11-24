using StaffSync.Domain.Common;

namespace StaffSync.Domain.Entities
{
    public class ContactCorrectionItem : EntityBase, IEntityBase
    {
        public int ContactCorrectionId { get; set; }
        public string FieldName { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }
}
