using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Results.CorrectionResults
{
    public class CorrectionDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public string ContactName { get; set; } = "";
        public Guid ReportedByUserId { get; set; }
        public string? Note { get; set; }
        public CorrectionStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CorrectionItemDto> Items { get; set; } = new();
    }

    public class CorrectionItemDto
    {
        public string FieldName { get; set; } = "";
        public string Label { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }
}
