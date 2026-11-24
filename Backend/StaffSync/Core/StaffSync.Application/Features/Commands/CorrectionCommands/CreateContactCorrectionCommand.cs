using MediatR;
using System.Text.Json.Serialization;

namespace StaffSync.Application.Features.Commands.CorrectionCommands
{
    public class CreateContactCorrectionCommand : IRequest<Unit>
    {
        [JsonIgnore] // Route'tan gelir, gövdeden alınmaz.
        public int ContactId { get; set; }
        public string? Note { get; set; }
        public List<CorrectionItemInput> Items { get; set; } = new();
    }

    public class CorrectionItemInput
    {
        public string FieldName { get; set; } = "";
        public string NewValue { get; set; } = "";
    }
}
