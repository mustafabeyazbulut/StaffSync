using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StaffSync.Models;

namespace StaffSync.ViewModels
{
    public partial class ContactDetailViewModel : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        public partial ContactDto? Contact { get; set; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Contact", out var cObj) && cObj is ContactDto c)
            {
                Contact = c;
            }
        }

        [RelayCommand]
        private void CallPhone()
        {
            if (Contact == null || string.IsNullOrWhiteSpace(Contact.TelephoneNumber))
                return;

            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open(Contact.TelephoneNumber);
            }
            catch { }
        }

        [RelayCommand]
        private async Task SendSmsAsync()
        {
            if (Contact == null || string.IsNullOrWhiteSpace(Contact.TelephoneNumber))
                return;

            try
            {
                if (Sms.Default.IsComposeSupported)
                    await Sms.Default.ComposeAsync(new SmsMessage("", Contact.TelephoneNumber));
            }
            catch { }
        }

        [RelayCommand]
        private async Task SendEmailAsync()
        {
            if (Contact == null || string.IsNullOrWhiteSpace(Contact.Email))
                return;

            try
            {
                if (Email.Default.IsComposeSupported)
                    await Email.Default.ComposeAsync(new EmailMessage
                    {
                        To = new List<string> { Contact.Email }
                    });
            }
            catch { }
        }

        [RelayCommand]
        private async Task OpenWhatsAppAsync()
        {
            if (Contact == null || string.IsNullOrWhiteSpace(Contact.TelephoneNumber))
                return;

            try
            {
                var cleanPhone = new string(Contact.TelephoneNumber.Where(char.IsDigit).ToArray());
                // Türkiye numaralarında baştaki 0'ı 90'a tamamla
                if (cleanPhone.StartsWith("0"))
                    cleanPhone = "9" + cleanPhone;
                else if (!cleanPhone.StartsWith("90") && cleanPhone.Length == 10)
                    cleanPhone = "90" + cleanPhone;

                var waUrl = $"https://wa.me/{cleanPhone}";
                await Launcher.Default.OpenAsync(new Uri(waUrl));
            }
            catch { }
        }

        [RelayCommand]
        private async Task SaveToContactsAsync()
        {
            if (Contact == null)
                return;

            if (Application.Current?.Windows[0].Page is Page page)
            {
                await page.DisplayAlertAsync(
                    "Rehbere Kaydet",
                    $"{Contact.DisplayName} cihazınızın rehberine eklensin mi?",
                    "Evet, Kaydet",
                    "İptal");
            }
        }

        [RelayCommand]
        private async Task ReportCorrectionAsync()
        {
            if (Contact == null)
                return;

            await Shell.Current.GoToAsync("report-correction", new Dictionary<string, object>
            {
                ["Contact"] = Contact
            });
        }
    }
}
