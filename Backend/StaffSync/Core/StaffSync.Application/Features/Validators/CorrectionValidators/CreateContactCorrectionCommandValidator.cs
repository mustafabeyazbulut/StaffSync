using FluentValidation;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Corrections;

namespace StaffSync.Application.Features.Validators.CorrectionValidators
{
    public class CreateContactCorrectionCommandValidator : AbstractValidator<CreateContactCorrectionCommand>
    {
        public CreateContactCorrectionCommandValidator()
        {
            RuleFor(x => x.Items).NotEmpty().WithMessage("En az bir alan seçmelisin.");
            RuleFor(x => x.Items).Must(items => items.Select(i => i.FieldName).Distinct().Count() == items.Count)
                .WithMessage("Aynı alan birden fazla seçilemez.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.FieldName).Must(ContactFields.IsKnown).WithMessage("Geçersiz alan.");
                item.RuleFor(i => i.NewValue).NotEmpty().WithMessage("Doğru değeri yazmalısın.").MaximumLength(200);
            });
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }
}
