using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Corrections;
using StaffSync.Application.Features.Rules.CorrectionRules;
using StaffSync.Application.Interfaces.Email;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.CorrectionHandlers
{
    public class CreateContactCorrectionCommandHandler : BaseHandler, IRequestHandler<CreateContactCorrectionCommand, Unit>
    {
        private readonly CorrectionRules rules;
        private readonly UserManager<User> userManager;
        private readonly IEmailSender emailSender;
        private readonly CorrectionSettings settings;
        private readonly ILogger<CreateContactCorrectionCommandHandler> logger;

        public CreateContactCorrectionCommandHandler(CorrectionRules rules, UserManager<User> userManager, IEmailSender emailSender,
            IOptions<CorrectionSettings> settings, ILogger<CreateContactCorrectionCommandHandler> logger,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
        {
            this.rules = rules;
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.settings = settings.Value;
            this.logger = logger;
        }

        public async Task<Unit> Handle(CreateContactCorrectionCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.ContactId && !x.IsDeleted);
            await rules.ContactMustExist(contact);

            Guid reporterId = Guid.Parse(userId!); // [Authorize] uçta olduğu için dolu
            DateTime oneHourAgo = DateTime.Now.AddHours(-1); // CreatedDate yerel saatle tutuluyor (EntityBase)
            int recent = await unitOfWork.GetReadRepository<ContactCorrection>()
                .CountAsync(x => x.ReportedByUserId == reporterId && x.CreatedDate >= oneHourAgo);
            await rules.MustNotExceedHourlyLimit(recent);
            await rules.ValuesMustChange(contact!, request.Items);

            var correction = new ContactCorrection
            {
                ContactId = contact!.Id,
                ReportedByUserId = reporterId,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                Items = request.Items.Select(i => new ContactCorrectionItem
                {
                    FieldName = i.FieldName,
                    OldValue = ContactFields.CurrentValue(contact, i.FieldName),
                    NewValue = i.NewValue.Trim()
                }).ToList()
            };
            await unitOfWork.GetWriteRepository<ContactCorrection>().AddAysnc(correction);
            await unitOfWork.SaveAsync();

            await NotifyAsync(contact, correction, cancellationToken);
            return Unit.Value;
        }

        // E-posta hatası bildirimi geri almaz: kayıt zaten DB'de, admin listeden görür.
        private async Task NotifyAsync(Contact contact, ContactCorrection correction, CancellationToken cancellationToken)
        {
            if (settings.NotifyEmails.Count == 0) return;
            try
            {
                User? reporter = await userManager.FindByIdAsync(correction.ReportedByUserId.ToString());
                var (subject, body) = CorrectionEmailBuilder.Build(contact, reporter?.FullName ?? "Bilinmeyen kullanıcı",
                    reporter?.Email ?? "", correction.Items, correction.Note);
                await emailSender.SendAsync(settings.NotifyEmails, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Düzeltme bildirimi {CorrectionId} için e-posta gönderilemedi", correction.Id);
            }
        }
    }
}
