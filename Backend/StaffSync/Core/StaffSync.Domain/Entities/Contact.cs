
using StaffSync.Domain.Common;

namespace StaffSync.Domain.Entities
{
    public class Contact: EntityBase,IEntityBase
    {
        public string CoverImageUrl { get; set; }
        public string LogonName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Office { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Company { get; set; }
        public string ManagerName { get; set; }
        public string Email { get; set; }
        public string City { get; set; }
        public string TelephoneNumber { get; set; }
        public string Home { get; set; } //KısaKod
        public string Mobile { get; set; } //Cep

    }
}
