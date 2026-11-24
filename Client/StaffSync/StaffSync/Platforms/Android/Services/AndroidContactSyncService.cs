#if ANDROID
using Android.Content;
using Android.Provider;
using StaffSync.Models;
using StaffSync.Services;

namespace StaffSync.Platforms.Android.Services
{
    public class AndroidContactSyncService : IContactSyncService
    {
        private const string AccountName = "StaffSync";
        private const string AccountType = "com.staffsync";

        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;

        public AndroidContactSyncService(IContactsService contactsService, ISettingsService settingsService)
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

            var context = global::Android.App.Application.Context;
            var resolver = context.ContentResolver;
            if (resolver == null)
                return 0;

            // 1. Silinen kişileri temizle
            foreach (var deletedId in changes.DeletedIds)
            {
                DeleteRawContact(resolver, deletedId);
            }

            // 2. Eklenen ve güncellenen kişileri yaz
            int count = 0;
            foreach (var contact in changes.Upserted)
            {
                UpsertContact(resolver, contact);
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

        private void DeleteRawContact(ContentResolver resolver, int contactId)
        {
            try
            {
                var deleteUri = ContactsContract.RawContacts.ContentUri!.BuildUpon()!
                    .AppendQueryParameter(ContactsContract.CallerIsSyncadapter, "true")!
                    .Build()!;

                var where = $"{ContactsContract.RawContacts.InterfaceConsts.AccountType} = ? AND {ContactsContract.RawContacts.InterfaceConsts.Sync1} = ?";
                resolver.Delete(deleteUri, where, new[] { AccountType, contactId.ToString() });
            }
            catch { }
        }

        private void UpsertContact(ContentResolver resolver, ContactDto contact)
        {
            try
            {
                // Önce varsa eskiyi sil
                DeleteRawContact(resolver, contact.Id);

                // Yeni ekle
                var ops = new List<ContentProviderOperation>();

                var rawContactBuilder = ContentProviderOperation.NewInsert(ContactsContract.RawContacts.ContentUri!)
                    .WithValue(ContactsContract.RawContacts.InterfaceConsts.AccountName, AccountName)
                    .WithValue(ContactsContract.RawContacts.InterfaceConsts.AccountType, AccountType)
                    .WithValue(ContactsContract.RawContacts.InterfaceConsts.Sync1, contact.Id.ToString());
                ops.Add(rawContactBuilder.Build());

                // İsim
                ops.Add(ContentProviderOperation.NewInsert(ContactsContract.Data.ContentUri!)
                    .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, 0)
                    .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.StructuredName.ContentItemType)
                    .WithValue(ContactsContract.CommonDataKinds.StructuredName.DisplayName, contact.DisplayName)
                    .Build());

                // Telefon
                if (!string.IsNullOrWhiteSpace(contact.TelephoneNumber))
                {
                    ops.Add(ContentProviderOperation.NewInsert(ContactsContract.Data.ContentUri!)
                        .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, 0)
                        .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Phone.ContentItemType)
                        .WithValue(ContactsContract.CommonDataKinds.Phone.Number, contact.TelephoneNumber)
                        .WithValue(ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type, (int)PhoneDataKind.Mobile)
                        .Build());
                }

                // E-posta
                if (!string.IsNullOrWhiteSpace(contact.Email))
                {
                    ops.Add(ContentProviderOperation.NewInsert(ContactsContract.Data.ContentUri!)
                        .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, 0)
                        .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Email.ContentItemType)
                        .WithValue(ContactsContract.CommonDataKinds.Email.Address, contact.Email)
                        .WithValue(ContactsContract.CommonDataKinds.Email.InterfaceConsts.Type, (int)EmailDataKind.Work)
                        .Build());
                }

                // Şirket & Unvan
                if (!string.IsNullOrWhiteSpace(contact.Company) || !string.IsNullOrWhiteSpace(contact.JobTitle) || !string.IsNullOrWhiteSpace(contact.Department))
                {
                    ops.Add(ContentProviderOperation.NewInsert(ContactsContract.Data.ContentUri!)
                        .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, 0)
                        .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Organization.ContentItemType)
                        .WithValue(ContactsContract.CommonDataKinds.Organization.Company, contact.Company ?? "AUNDE")
                        .WithValue(ContactsContract.CommonDataKinds.Organization.Title, contact.JobTitle ?? "")
                        .WithValue(ContactsContract.CommonDataKinds.Organization.Department, contact.Department ?? "")
                        .Build());
                }

                // Dahili / Not
                if (!string.IsNullOrWhiteSpace(contact.Home))
                {
                    ops.Add(ContentProviderOperation.NewInsert(ContactsContract.Data.ContentUri!)
                        .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, 0)
                        .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Note.ContentItemType)
                        .WithValue(ContactsContract.IDataColumns.Data1, $"Dahili / Kısa Kod: {contact.Home}")
                        .Build());
                }

                resolver.ApplyBatch(ContactsContract.Authority, ops);
            }
            catch { }
        }

        public Task ClearSyncedContactsAsync(CancellationToken ct = default)
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var resolver = context.ContentResolver;
                if (resolver != null)
                {
                    var deleteUri = ContactsContract.RawContacts.ContentUri!.BuildUpon()!
                        .AppendQueryParameter(ContactsContract.CallerIsSyncadapter, "true")!
                        .Build()!;

                    var where = $"{ContactsContract.RawContacts.InterfaceConsts.AccountType} = ?";
                    resolver.Delete(deleteUri, where, new[] { AccountType });
                }
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
