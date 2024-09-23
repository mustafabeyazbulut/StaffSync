﻿using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Commands.AuthCommands
{
    public class RevokeCommand :IRequest<Unit>
    {
        public string Email { get; set; }
    }
}
