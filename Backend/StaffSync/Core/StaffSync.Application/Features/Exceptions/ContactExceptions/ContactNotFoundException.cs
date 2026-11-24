using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.ContactExceptions
{
    public class ContactNotFoundException : NotFoundException
    {
        public ContactNotFoundException() : base("Kişi bulunamadı.") { }
    }
}
