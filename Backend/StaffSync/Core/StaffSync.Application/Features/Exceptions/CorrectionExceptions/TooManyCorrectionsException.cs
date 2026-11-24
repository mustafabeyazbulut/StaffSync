using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.CorrectionExceptions
{
    public class TooManyCorrectionsException : BaseException
    {
        public TooManyCorrectionsException() : base("Son bir saatte çok fazla bildirim gönderdin, biraz sonra tekrar dene.") { }
    }
}
