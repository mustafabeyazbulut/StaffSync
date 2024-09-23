using FluentValidation;
using StaffSync.Application.Features.Results.AuthResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Validators.AuthValidators
{
    public class RefreshTokenCommandValidator :AbstractValidator<RefreshTokenCommandResult>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.AccessToken).NotEmpty();

            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
