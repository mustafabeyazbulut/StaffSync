using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.CorrectionExceptions
{
    public class CorrectionValueUnchangedException : BaseException
    {
        public CorrectionValueUnchangedException(string label) : base($"{label} için yazdığın değer mevcut değerle aynı.") { }
    }
}
