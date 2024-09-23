using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Results.AuthResults
{
    public class RefreshTokenCommandResult
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
