using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StaffSync.Messages
{
    /// <summary>Profil'de "Şirket hattı kullanıyorum" değişince gönderilir; ara düğmeleri güncellenir.</summary>
    public class CompanyLineChangedMessage : ValueChangedMessage<bool>
    {
        public CompanyLineChangedMessage(bool usesCompanyLine) : base(usesCompanyLine)
        {
        }
    }
}
