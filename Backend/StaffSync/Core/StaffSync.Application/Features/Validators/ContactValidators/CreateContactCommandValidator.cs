using FluentValidation;
using StaffSync.Application.Features.Mediator.Commands.ContactCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
    }
}
