using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StaffSync.Models;

namespace StaffSync.ViewModels
{
    public partial class ContactDetailViewModel : ObservableObject, IQueryAttributable
    {
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
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private void CallShortCode()
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            if (Contact == null || string.IsNullOrWhiteSpace(Contact.Home))
                return;

            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open(Contact.Home);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dahili arama başlatılamadı: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CallPhone()
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            if (Contact == null || string.IsNullOrWhiteSpace(Contact.TelephoneNumber))
                return;

            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open(Contact.TelephoneNumber);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Telefon araması başlatılamadı: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SendSmsAsync()
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
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
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
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
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
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
                if (!string.IsNullOrWhiteSpace(Contact.TelephoneNumber))
                {
                    intent.PutExtra(Android.Provider.ContactsContract.Intents.Insert.Phone, Contact.TelephoneNumber);
                    intent.PutExtra(Android.Provider.ContactsContract.Intents.Insert.PhoneType, (int)Android.Provider.PhoneDataKind.Mobile);
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
                if (!string.IsNullOrWhiteSpace(Contact.TelephoneNumber))
                {
                    newContact.PhoneNumbers = new[]
                    {
                        new Contacts.CNLabeledValue<Contacts.CNPhoneNumber>(
                            Contacts.CNLabelPhoneNumberKey.Mobile,
                            new Contacts.CNPhoneNumber(Contact.TelephoneNumber))
                    };
                }
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
                if (Application.Current?.Windows[0].Page is Page page)
                {
                    await page.DisplayAlertAsync(
                        "Bilgi",
                        "Bu platformda rehbere kaydetme desteklenmemektedir.",
                        "Tamam");
                }
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Rehbere kaydetme hatası: {ex}");
                if (Application.Current?.Windows[0].Page is Page page)
                {
                    await page.DisplayAlertAsync(
                        "Hata",
                        "Rehber ekranı açılamadı: " + ex.Message,
                        "Tamam");
                }
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
