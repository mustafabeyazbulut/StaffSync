using MediatR;
using StaffSync.Application.Features.Results.AuthResults;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Commands.AuthCommands
{
    public class LoginCommand : IRequest<LoginCommandResult>
    {
        [DefaultValue("mustafabeyazbulut@gmail.com")]
        public string Email { get; set; }
        [DefaultValue("123456")]
        public string Password { get; set; }
    }
}
