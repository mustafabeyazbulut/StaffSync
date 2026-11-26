using FluentValidation;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Sessions;

namespace StaffSync.Application.Features.Validators.AuthValidators
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.DeviceId).MaximumLength(SessionTokens.DeviceIdMaxLength);
        }
    }
}
