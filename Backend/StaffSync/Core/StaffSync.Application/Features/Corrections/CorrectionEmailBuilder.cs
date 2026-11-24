using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.Corrections
{
    public static class CorrectionEmailBuilder
    {
        public static (string Subject, string Body) Build(Contact contact, string reporterName, string reporterEmail,
            IEnumerable<ContactCorrectionItem> items, string? note)
        {
            var body = new StringBuilder()
                .AppendLine($"{reporterName} ({reporterEmail}) şu kişi için düzeltme önerdi: {contact.DisplayName} (#{contact.Id})")
                .AppendLine();

            foreach (var item in items)
            {
                string oldValue = string.IsNullOrEmpty(item.OldValue) ? "(boş)" : item.OldValue;
                body.AppendLine($"- {ContactFields.Label(item.FieldName)}: {oldValue} → {item.NewValue}");
            }

            if (!string.IsNullOrWhiteSpace(note))
                body.AppendLine().AppendLine($"Not: {note}");

            body.AppendLine().AppendLine("Kişiyi güncellemek için StaffSync yönetim uçlarını kullanın.");
            return ($"StaffSync düzeltme önerisi: {contact.DisplayName}", body.ToString());
        }
    }
}
