using MediatR;
using StaffSync.Application.Features.Results.AuthResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Commands.AuthCommands
{
    public class RefreshTokenCommand : IRequest<RefreshTokenCommandResult>
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
