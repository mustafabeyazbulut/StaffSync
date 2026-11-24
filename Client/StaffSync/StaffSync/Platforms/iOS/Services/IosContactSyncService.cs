#if IOS
using Contacts;
using Foundation;
using StaffSync.Models;
using StaffSync.Services;

namespace StaffSync.Platforms.iOS.Services
{
    public class IosContactSyncService : IContactSyncService
    {
        private const string TagPrefix = "StaffSync:";
        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;

        public IosContactSyncService(IContactsService contactsService, ISettingsService settingsService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;
        }

        public bool IsSupported => true;

        public async Task<int> SyncContactsAsync(CancellationToken ct = default)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.ContactsWrite>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.ContactsWrite>();
                if (status != PermissionStatus.Granted)
                {
                    _settingsService.LastSyncError = "Rehber yazma izni verilmedi.";
                    return 0;
                }
            }

            var changes = await _contactsService.GetChangesAsync(_settingsService.LastSyncTime);
            if (changes == null)
            {
                _settingsService.LastSyncError = "Sunucudan değişiklikler alınamadı.";
                return 0;
            }

            using var store = new CNContactStore();

            // 1. Silinen kişileri temizle
            foreach (var deletedId in changes.DeletedIds)
            {
                DeleteContact(store, deletedId);
            }

            // 2. Eklenen ve güncellenenleri kaydet
            int count = 0;
            foreach (var contact in changes.Upserted)
            {
                UpsertContact(store, contact);
                count++;
            }

            // Önbelleği güncelle
            var cached = await _settingsService.GetCachedContactsAsync();
            cached.RemoveAll(c => changes.DeletedIds.Contains(c.Id));
            foreach (var item in changes.Upserted)
            {
                var idx = cached.FindIndex(c => c.Id == item.Id);
                if (idx >= 0) cached[idx] = item;
                else cached.Add(item);
            }
            await _settingsService.SaveCachedContactsAsync(cached);

            _settingsService.LastSyncTime = changes.ServerTimeUtc;
            _settingsService.LastSyncError = null;

            return count;
        }

        private void DeleteContact(CNContactStore store, int contactId)
        {
            try
            {
                var tag = $"{TagPrefix}{contactId}";
                var keysToFetch = new[] { CNContactKey.Note };
                var fetchRequest = new CNContactFetchRequest(keysToFetch);

                NSError? error = null;
                store.EnumerateContacts(fetchRequest, out error, (contact, cursor) =>
                {
                    if (contact.Note != null && contact.Note.Contains(tag))
                    {
                        var req = new CNSaveRequest();
                        req.DeleteContact(contact.MutableCopy() as CNMutableContact);
                        store.ExecuteSaveRequest(req, out _);
                    }
                });
            }
            catch { }
        }

        private void UpsertContact(CNContactStore store, ContactDto contact)
        {
            try
            {
                // Varsa eskiyi sil
                DeleteContact(store, contact.Id);

                var newContact = new CNMutableContact
                {
                    GivenName = contact.DisplayName,
                    OrganizationName = contact.Company ?? "AUNDE",
                    DepartmentName = contact.Department ?? "",
                    JobTitle = contact.JobTitle ?? "",
                    Note = $"{TagPrefix}{contact.Id} | Dahili: {contact.Home ?? ""}"
                };

                if (!string.IsNullOrWhiteSpace(contact.TelephoneNumber))
                {
                    newContact.PhoneNumbers = new[]
                    {
                        new CNLabeledValue<CNPhoneNumber>(
                            CNLabelPhoneNumberKey.Mobile,
                            new CNPhoneNumber(contact.TelephoneNumber))
                    };
                }

                if (!string.IsNullOrWhiteSpace(contact.Email))
                {
                    newContact.EmailAddresses = new[]
                    {
                        new CNLabeledValue<NSString>(
                            CNLabelKey.Work,
                            new NSString(contact.Email))
                    };
                }

                var saveRequest = new CNSaveRequest();
                saveRequest.AddContact(newContact, null);
                store.ExecuteSaveRequest(saveRequest, out _);
            }
            catch { }
        }

        public Task ClearSyncedContactsAsync(CancellationToken ct = default)
        {
            try
            {
                using var store = new CNContactStore();
                var keysToFetch = new[] { CNContactKey.Note };
                var fetchRequest = new CNContactFetchRequest(keysToFetch);

                store.EnumerateContacts(fetchRequest, out _, (contact, cursor) =>
                {
                    if (contact.Note != null && contact.Note.Contains(TagPrefix))
                    {
                        var req = new CNSaveRequest();
                        req.DeleteContact(contact.MutableCopy() as CNMutableContact);
                        store.ExecuteSaveRequest(req, out _);
                    }
                });
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = ex.Message;
            }

            _settingsService.LastSyncTime = null;
            return Task.CompletedTask;
        }
    }
}
#endif
