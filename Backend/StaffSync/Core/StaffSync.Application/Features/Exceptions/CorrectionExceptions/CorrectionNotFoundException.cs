using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.CorrectionExceptions
{
    public class CorrectionNotFoundException : NotFoundException
    {
        public CorrectionNotFoundException() : base("Bildirim bulunamadı.") { }
    }
}
