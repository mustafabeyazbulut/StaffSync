using FluentValidation;
using StaffSync.Application.Features.Commands.ContactCommands;

namespace StaffSync.Application.Features.Validators.ContactValidators
{
    public class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
    {
        public CreateContactCommandValidator()
        {
            RuleFor(x=>x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithName("Email");

            RuleFor(x => x.TelephoneNumber)
                .NotEmpty().WithMessage("{TelephoneNumber} cannot be empty")
                .Matches(@"^\+?\d{10,15}$").WithMessage("{TelephoneNumber} must be a valid phone number")
                .WithName("TelephoneNumber");
        }
    }
}
