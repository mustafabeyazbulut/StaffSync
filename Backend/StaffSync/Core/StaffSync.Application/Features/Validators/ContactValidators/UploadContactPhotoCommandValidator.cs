using FluentValidation;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Photos;

namespace StaffSync.Application.Features.Validators.ContactValidators
{
    public class UploadContactPhotoCommandValidator : AbstractValidator<UploadContactPhotoCommand>
    {
        public UploadContactPhotoCommandValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Fotoğraf dosyası boş.")
                .Must(c => c.Length <= ImageFormats.MaxPhotoBytes)
                    .WithMessage("Fotoğraf en fazla 2 MB olabilir.")
                .Must(c => ImageFormats.DetectContentType(c) != null)
                    .WithMessage("Fotoğraf JPEG ya da PNG olmalı.");
        }
    }
}
