using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.ContactExceptions
{
    public class ContactPhotoNotFoundException : NotFoundException
    {
        public ContactPhotoNotFoundException() : base("Kişinin fotoğrafı yok.") { }
    }
}
