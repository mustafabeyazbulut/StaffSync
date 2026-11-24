namespace StaffSync.Application.Features.Corrections
{
    // appsettings: "Corrections": { "NotifyEmails": [ "ik@...", "bt@..." ] }
    public class CorrectionSettings
    {
        public List<string> NotifyEmails { get; set; } = new();
    }
}
