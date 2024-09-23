using FluentValidation;
using StaffSync.Application.Features.Commands.AuthCommands;


namespace StaffSync.Application.Features.Validators.AuthValidators
{
    public  class RevokeCommandValidator : AbstractValidator<RevokeCommand>
    {
        public RevokeCommandValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .NotEmpty();
        }
    }
}
