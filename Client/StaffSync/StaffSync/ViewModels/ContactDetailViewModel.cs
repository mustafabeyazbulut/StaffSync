using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StaffSync.Models;
using StaffSync.Services;
using StaffSync.Utils;

namespace StaffSync.ViewModels
{
    public partial class ContactDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ICallService _callService;
        private readonly ISettingsService _settingsService;
        private readonly IContactPhotoService _photoService;
        private readonly IDialogService _dialogService;

        public ContactDetailViewModel(ICallService callService, ISettingsService settingsService,
            IContactPhotoService photoService, IDialogService dialogService)
        {
            _dialogService = dialogService;
            _callService = callService;
            _settingsService = settingsService;
            _photoService = photoService;
        }

        [ObservableProperty]
        public partial ContactDto? Contact { get; set; }

        public bool HasOffice => !string.IsNullOrWhiteSpace(Contact?.Office);
        public bool HasCompany => !string.IsNullOrWhiteSpace(Contact?.Company);
        public bool HasManager => !string.IsNullOrWhiteSpace(Contact?.ManagerName);
        public bool HasDepartment => !string.IsNullOrWhiteSpace(Contact?.Department);

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Contact", out var cObj) && cObj is ContactDto c)
            {
                Contact = c;
                OnPropertyChanged(nameof(HasOffice));
                OnPropertyChanged(nameof(HasCompany));
                OnPropertyChanged(nameof(HasManager));
                OnPropertyChanged(nameof(HasDepartment));

                // Listeden gelirken fotoğraf genelde inmiş olur; inmediyse burada indir
                if (c.PhotoPath == null && c.HasPhotoUrl)
                    _ = _photoService.LoadPhotosAsync(new[] { c });
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>Üstteki "Ara": şirket hattı ayarına göre kısa kod ya da cep.</summary>
        [RelayCommand]
        private void Call()
        {
            if (Contact != null)
                _callService.Call(Contact);
        }

        /// <summary>"Dahili / kısa kod" satırı: her zaman kısa kodu arar.</summary>
        [RelayCommand]
        private void CallShortCode() => _callService.Dial(Contact?.Home?.Trim());

        /// <summary>"Cep telefonu" satırı: her zaman cep numarasını arar.</summary>
        [RelayCommand]
        private void CallPhone() => _callService.Dial(Contact?.TelephoneNumber);

        [RelayCommand]
        private async Task SendSmsAsync()
        {
            Haptics.Click();
            if (Contact == null || string.IsNullOrWhiteSpace(Contact.TelephoneNumber))
                return;

            try
            {
                if (Sms.Default.IsComposeSupported)
                    await Sms.Default.ComposeAsync(new SmsMessage("", Contact.TelephoneNumber));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS başlatılamadı: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SendEmailAsync()
        {
            Haptics.Click();
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"E-posta başlatılamadı: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenWhatsAppAsync()
        {
            Haptics.Click();
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WhatsApp açılamadı: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveToContactsAsync()
        {
            if (Contact == null)
                return;

            try
            {
#if ANDROID
                var intent = new Android.Content.Intent(Android.Provider.ContactsContract.Intents.Insert.Action);
                intent.SetType(Android.Provider.ContactsContract.RawContacts.ContentType);
                intent.PutExtra(Android.Provider.ContactsContract.Intents.Insert.Name, Contact.DisplayName);

                // Numaralar Insert.Data satırlarıyla verilir: kısa kodun "Kısa kod" etiketi ancak böyle taşınır.
                // Şirket hattı kullanan için kısa kod ilk sırada; cep her zaman (arayan kimliği için).
                var phoneRows = new List<Android.OS.IParcelable>();
                if (_settingsService.UsesCompanyLine && Contact.HasShortCode)
                {
                    var shortCode = new Android.Content.ContentValues();
                    shortCode.Put(Android.Provider.ContactsContract.Data.InterfaceConsts.Mimetype, Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentItemType);
                    shortCode.Put(Android.Provider.ContactsContract.CommonDataKinds.Phone.Number, Contact.Home!.Trim());
                    shortCode.Put(Android.Provider.ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type, (int)Android.Provider.PhoneDataKind.Custom);
                    shortCode.Put(Android.Provider.ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Label, DisplayText.ShortCodeLabel);
                    phoneRows.Add(shortCode);
                }
                if (Contact.HasPhone)
                {
                    var mobile = new Android.Content.ContentValues();
                    mobile.Put(Android.Provider.ContactsContract.Data.InterfaceConsts.Mimetype, Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentItemType);
                    mobile.Put(Android.Provider.ContactsContract.CommonDataKinds.Phone.Number, Contact.TelephoneNumber);
                    mobile.Put(Android.Provider.ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type, (int)Android.Provider.PhoneDataKind.Mobile);
                    phoneRows.Add(mobile);
                }
                if (phoneRows.Count > 0)
                {
                    intent.PutParcelableArrayListExtra(Android.Provider.ContactsContract.Intents.Insert.Data, phoneRows);
                }
                if (!string.IsNullOrWhiteSpace(Contact.Email))
                {
                    intent.PutExtra(Android.Provider.ContactsContract.Intents.Insert.Email, Contact.Email);
                    intent.PutExtra(Android.Provider.ContactsContract.Intents.Insert.EmailType, (int)Android.Provider.EmailDataKind.Work);
                }
                if (!string.IsNullOrWhiteSpace(Contact.Company))
                {
                    intent.PutExtra(Android.Provider.ContactsContract.Intents.Insert.Company, Contact.Company);
                }
                if (!string.IsNullOrWhiteSpace(Contact.JobTitle))
                {
                    intent.PutExtra(Android.Provider.ContactsContract.Intents.Insert.JobTitle, Contact.JobTitle);
                }
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
#elif IOS
                var newContact = new Contacts.CNMutableContact
                {
                    GivenName = Contact.DisplayName,
                    OrganizationName = Contact.Company ?? "",
                    JobTitle = Contact.JobTitle ?? "",
                    DepartmentName = Contact.Department ?? ""
                };
                newContact.PhoneNumbers = Platforms.iOS.Services.IosContactSyncService.BuildPhoneNumbers(
                    Contact, _settingsService.UsesCompanyLine);
                if (!string.IsNullOrWhiteSpace(Contact.Email))
                {
                    newContact.EmailAddresses = new[]
                    {
                        new Contacts.CNLabeledValue<Foundation.NSString>(
                            Contacts.CNLabelKey.Work,
                            new Foundation.NSString(Contact.Email))
                    };
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var controller = ContactsUI.CNContactViewController.FromNewContact(newContact);
                    var navController = new UIKit.UINavigationController(controller);
                    UIKit.UIViewController? rootVc = null;
#pragma warning disable CA1416, CA1422
                    if (OperatingSystem.IsIOSVersionAtLeast(13))
                    {
                        var window = UIKit.UIApplication.SharedApplication.ConnectedScenes
                            .OfType<UIKit.UIWindowScene>()
                            .SelectMany(s => s.Windows)
                            .FirstOrDefault(w => w.IsKeyWindow);
                        rootVc = window?.RootViewController;
                    }
                    else
                    {
                        rootVc = UIKit.UIApplication.SharedApplication.KeyWindow?.RootViewController;
                    }
#pragma warning restore CA1416, CA1422
                    while (rootVc?.PresentedViewController != null)
                    {
                        rootVc = rootVc.PresentedViewController;
                    }
                    rootVc?.PresentViewController(navController, true, null);
                });
#else
                await _dialogService.AlertAsync(
                    "Bu cihazda kullanılamıyor",
                    "Rehbere kaydetme bu cihazda desteklenmiyor.");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Rehbere kaydetme hatası: {ex}");
                await _dialogService.AlertAsync(
                    "Rehber açılamadı",
                    "Kişiyi kaydetmek için telefon rehberi açılamadı. Lütfen tekrar deneyin.",
                    DialogKind.Error);
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
