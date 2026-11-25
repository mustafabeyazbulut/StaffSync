using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.AuthExceptions
{
    public class RefreshTokenShouldBeValidException : BaseException
    {
        public RefreshTokenShouldBeValidException() : base("Oturum geçersiz. Lütfen tekrar giriş yapın.") { }
    }
}
