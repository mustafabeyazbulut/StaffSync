using MediatR;
using StaffSync.Domain.Entities;
using System.Text.Json.Serialization;

namespace StaffSync.Application.Features.Commands.CorrectionCommands
{
    public class UpdateCorrectionStatusCommand : IRequest
    {
        [JsonIgnore]
        public int Id { get; set; }
        public CorrectionStatus Status { get; set; }
    }
}
