using StaffSync.Models;

namespace StaffSync.Services
{
    /// <summary>
    /// Bir kişiyi aramanın tek yeri. Kullanıcı şirket hattı kullanıyorsa kısa kod, kullanmıyorsa
    /// cep numarası aranır; ekranlar bu kararı kendileri vermez.
    /// </summary>
    public interface ICallService
    {
        /// <summary>Bu kullanıcı için kişinin aranacak numarası; aranabilecek numara yoksa null.</summary>
        string? GetCallNumber(ContactDto contact);

        bool CanCall(ContactDto contact);

        /// <summary>Ara düğmesinde gösterilecek metin: şirket hattında kısa kod, değilse "Ara".</summary>
        string GetCallLabel(ContactDto contact);

        void Call(ContactDto contact);

        /// <summary>Belirli bir numarayı arar (detaydaki "Cep telefonu" / "Kısa kod" satırları).</summary>
        void Dial(string? number);
    }

    public class CallService : ICallService
    {
        private readonly ISettingsService _settingsService;

        public CallService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public string? GetCallNumber(ContactDto contact)
        {
            if (_settingsService.UsesCompanyLine && contact.HasShortCode)
                return contact.Home!.Trim();
            return contact.HasPhone ? contact.TelephoneNumber : null;
        }

        public bool CanCall(ContactDto contact) => GetCallNumber(contact) != null;

        public string GetCallLabel(ContactDto contact) =>
            _settingsService.UsesCompanyLine && contact.HasShortCode ? contact.Home!.Trim() : "Ara";

        public void Call(ContactDto contact) => Dial(GetCallNumber(contact));

        public void Dial(string? number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return;

            Utils.Haptics.Click();

            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open(number);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Arama başlatılamadı ({number}): {ex.Message}");
            }
        }
    }
}
