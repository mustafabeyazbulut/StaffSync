namespace StaffSync.Application.Bases
{
    // Aranan kayıt yoksa fırlatılır; ExceptionMiddleware 404 döner.
    public class NotFoundException : BaseException
    {
        public NotFoundException(string message) : base(message) { }
    }
}
