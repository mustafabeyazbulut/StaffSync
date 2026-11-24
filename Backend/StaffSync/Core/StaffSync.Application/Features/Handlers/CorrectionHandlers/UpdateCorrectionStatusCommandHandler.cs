using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Rules.CorrectionRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.CorrectionHandlers
{
    public class UpdateCorrectionStatusCommandHandler : BaseHandler, IRequestHandler<UpdateCorrectionStatusCommand>
    {
        private readonly CorrectionRules rules;

        public UpdateCorrectionStatusCommandHandler(CorrectionRules rules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
            this.rules = rules;
        }

        public async Task Handle(UpdateCorrectionStatusCommand request, CancellationToken cancellationToken)
        {
            ContactCorrection? correction = await unitOfWork.GetReadRepository<ContactCorrection>()
                .GetAsync(x => x.Id == request.Id, enableTracking: true);
            await rules.CorrectionMustExist(correction);

            correction!.Status = request.Status;
            await unitOfWork.SaveAsync();
        }
    }
}
