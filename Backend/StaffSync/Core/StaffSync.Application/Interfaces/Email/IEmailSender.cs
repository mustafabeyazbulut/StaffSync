namespace StaffSync.Application.Interfaces.Email
{
    public interface IEmailSender
    {
        Task SendAsync(IEnumerable<string> to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
