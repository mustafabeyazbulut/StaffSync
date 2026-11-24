using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Corrections
{
    // Bildirimde seçilebilen alanlar. Mobil uygulama FieldName olarak bu anahtarları gönderir.
    public static class ContactFields
    {
        public const string Other = "Other";

        private static readonly Dictionary<string, (string Label, Func<Contact, string?> Get)> fields = new(StringComparer.Ordinal)
        {
            ["DisplayName"] = ("Ad soyad", c => c.DisplayName),
            ["TelephoneNumber"] = ("Cep telefonu", c => c.TelephoneNumber),
            ["Home"] = ("Kısa kod", c => c.Home),
            ["Email"] = ("E-posta", c => c.Email),
            ["JobTitle"] = ("Unvan", c => c.JobTitle),
            ["Department"] = ("Departman", c => c.Department),
            ["Office"] = ("Ofis", c => c.Office),
            ["Company"] = ("Şirket", c => c.Company),
            ["ManagerName"] = ("Yönetici", c => c.ManagerName),
            [Other] = ("Diğer", _ => "")
        };

        public static bool IsKnown(string fieldName) => fields.ContainsKey(fieldName);
        public static string Label(string fieldName) => fields[fieldName].Label;
        public static string CurrentValue(Contact contact, string fieldName) => fields[fieldName].Get(contact) ?? "";
    }
}
