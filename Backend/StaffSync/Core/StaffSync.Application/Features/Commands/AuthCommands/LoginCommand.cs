using MediatR;
using StaffSync.Application.Features.Results.AuthResults;
using System.ComponentModel;

namespace StaffSync.Application.Features.Commands.AuthCommands
{
    public class LoginCommand : IRequest<LoginCommandResult>
    {
        [DefaultValue("mustafabeyazbulut@gmail.com")]
        public string Email { get; set; }
        [DefaultValue("789+Asdf")]
        public string Password { get; set; }

        /// <summary>
        /// Uygulamanın cihaza kurulumda ürettiği kalıcı kimlik. Oturum cihaz başına tutulur; boşsa
        /// ortak "default" oturum kullanılır.
        /// </summary>
        public string? DeviceId { get; set; }
    }
}
