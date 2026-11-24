using StaffSync.Application.Features.Corrections;
using StaffSync.Application.Features.Results.CorrectionResults;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mappings
{
    public static class CorrectionMappings
    {
        public static CorrectionDto ToDto(this ContactCorrection c) => new()
        {
            Id = c.Id,
            ContactId = c.ContactId,
            ContactName = c.Contact?.DisplayName ?? "",
            ReportedByUserId = c.ReportedByUserId,
            Note = c.Note,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            Items = c.Items.Select(i => new CorrectionItemDto
            {
                FieldName = i.FieldName,
                Label = ContactFields.Label(i.FieldName),
                OldValue = i.OldValue,
                NewValue = i.NewValue
            }).ToList()
        };
    }
}
