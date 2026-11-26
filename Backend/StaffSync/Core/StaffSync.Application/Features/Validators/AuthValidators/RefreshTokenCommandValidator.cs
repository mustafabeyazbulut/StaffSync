using FluentValidation;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Sessions;

namespace StaffSync.Application.Features.Validators.AuthValidators
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.AccessToken).NotEmpty();

            RuleFor(x => x.RefreshToken).NotEmpty();

            RuleFor(x => x.DeviceId).MaximumLength(SessionTokens.DeviceIdMaxLength);
        }
    }
}
