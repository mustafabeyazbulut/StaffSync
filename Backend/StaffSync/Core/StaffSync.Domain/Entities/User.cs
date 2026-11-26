using Microsoft.AspNetCore.Identity;

namespace StaffSync.Domain.Entities
{
    public class User: IdentityUser<Guid>
    {
        public string FullName { get; set; }
    }
}
