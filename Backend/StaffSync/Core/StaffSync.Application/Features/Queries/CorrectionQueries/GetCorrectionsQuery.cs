using MediatR;
using StaffSync.Application.Features.Results.CorrectionResults;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.CorrectionQueries
{
    public class GetCorrectionsQuery : IRequest<List<CorrectionDto>>
    {
        public CorrectionStatus? Status { get; set; }
    }
}
