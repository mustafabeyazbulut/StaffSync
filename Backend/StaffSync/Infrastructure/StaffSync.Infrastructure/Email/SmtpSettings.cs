namespace StaffSync.Infrastructure.Email
{
    // appsettings "Smtp" bölümü. Host boşsa e-posta gönderilmez (bildirim yine kaydedilir).
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string From { get; set; } = "";
    }
}
