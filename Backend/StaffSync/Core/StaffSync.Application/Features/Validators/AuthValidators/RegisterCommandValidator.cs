using FluentValidation;
using StaffSync.Application.Features.Commands.AuthCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Validators.AuthValidators
{
    public class RegisterCommandValidator :AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(50)
                .MinimumLength(2)
                .WithName("İsim Soyisim");

            RuleFor(x => x.Email)
                .NotEmpty()
                .MaximumLength(60)
                .EmailAddress()
                .MinimumLength(8)
                .WithName("E-posta Adresi");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[0-9]").WithMessage("Parola en az bir rakam içermelidir.")
                .Matches("[a-z]").WithMessage("Parola en az bir küçük harf içermelidir.")
                .Matches("[A-Z]").WithMessage("Parola en az bir büyük harf içermelidir.")
                .WithName("Parola");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .Equal(x => x.Password)
                .WithName("Parola Tekrarı");
        }
    }
}
