using Microsoft.AspNetCore.Http;
using StaffSync.Application.Interfaces.UnitOfWorks;
using System.Security.Claims;

namespace StaffSync.Application.Bases
{
    public class BaseHandler
    {
        public readonly IUnitOfWork unitOfWork;
        public readonly IHttpContextAccessor httpContextAccessor;
        // HTTP isteği dışında (arka plan servisi) null'dır.
        public readonly string? userId;

        public BaseHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            this.unitOfWork = unitOfWork;
            this.httpContextAccessor = httpContextAccessor;
            userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
