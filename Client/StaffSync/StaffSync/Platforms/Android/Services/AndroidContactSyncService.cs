#if ANDROID
using Android.Accounts;
using Android.Content;
using Android.OS;
using Android.Provider;
using StaffSync.Models;
using StaffSync.Services;

namespace StaffSync.Platforms.Android.Services
{
    public class AndroidContactSyncService : IContactSyncService
    {
        public const string AccountName = "StaffSync";
        public const string AccountType = "com.aunde.staffsync";

        private static readonly SemaphoreSlim _syncSemaphore = new(1, 1);

        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;

        public AndroidContactSyncService(IContactsService contactsService, ISettingsService settingsService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;
        }

        public bool IsSupported => true;

        private static global::Android.Net.Uri BuildSyncAdapterUri(global::Android.Net.Uri baseUri)
        {
            return baseUri.BuildUpon()!
                .AppendQueryParameter(ContactsContract.CallerIsSyncadapter, "true")!
                .AppendQueryParameter(ContactsContract.RawContacts.InterfaceConsts.AccountName, AccountName)!
                .AppendQueryParameter(ContactsContract.RawContacts.InterfaceConsts.AccountType, AccountType)!
                .Build()!;
        }

        public static void EnsureAccount(Context context)
        {
            var account = new Account(AccountName, AccountType);
            var accountManager = AccountManager.Get(context);
            if (accountManager != null)
            {
                var accounts = accountManager.GetAccountsByType(AccountType);
                if (accounts == null || accounts.Length == 0)
                {
                    accountManager.AddAccountExplicitly(account, null, null);
                }
            }

            ContentResolver.SetIsSyncable(account, ContactsContract.Authority, 1);
            ContentResolver.SetSyncAutomatically(account, ContactsContract.Authority, true);
            ContentResolver.AddPeriodicSync(account, ContactsContract.Authority, Bundle.Empty, 86400);
        }

        public async Task<int> SyncContactsAsync(CancellationToken ct = default)
        {
            if (!await _syncSemaphore.WaitAsync(0, ct))
            {
                System.Diagnostics.Debug.WriteLine("AndroidContactSyncService: Senkronizasyon zaten çalışıyor, çağrı atlandı.");
                return 0;
            }

            try
            {
                var writeStatus = await Permissions.CheckStatusAsync<Permissions.ContactsWrite>();
                var readStatus = await Permissions.CheckStatusAsync<Permissions.ContactsRead>();
                if (writeStatus != PermissionStatus.Granted || readStatus != PermissionStatus.Granted)
                {
                    _settingsService.LastSyncError = "Rehber okuma/yazma izni verilmedi.";
                    return 0;
                }

                var context = global::Android.App.Application.Context;
                EnsureAccount(context);

                var resolver = context.ContentResolver;
                if (resolver == null)
                {
                    _settingsService.LastSyncError = "ContentResolver bulunamadı.";
                    return 0;
                }

                var lastSync = _settingsService.LastSyncTime;
                var changes = await _contactsService.GetChangesAsync(lastSync);
                if (changes == null)
                {
                    _settingsService.LastSyncError = "Sunucudan değişiklikler alınamadı.";
                    return 0;
                }

                var cacheService = IPlatformApplication.Current?.Services.GetService<IContactCacheService>();
                if (cacheService != null)
                {
                    await cacheService.UpdateCacheWithChangesAsync(changes);
                }

                var deviceContacts = GetDeviceStaffSyncContacts(resolver);

                // 1. Silme uzlaştırması
                if (lastSync == null)
                {
                    // Tam senkronda: cihazda olup sunucuda olmayan kişileri sil
                    var serverIds = new HashSet<int>(changes.Upserted.Select(c => c.Id));
                    var staleRawIds = deviceContacts
                        .Where(kv => !serverIds.Contains(kv.Key))
                        .Select(kv => kv.Value)
                        .ToList();

                    if (staleRawIds.Count > 0)
                    {
                        foreach (var chunk in staleRawIds.Chunk(50))
                        {
                            var deleteOps = new List<ContentProviderOperation>();
                            foreach (var rawId in chunk)
                            {
                                deleteOps.Add(ContentProviderOperation.NewDelete(BuildSyncAdapterUri(ContactsContract.RawContacts.ContentUri!))
                                    .WithSelection($"{ContactsContract.RawContacts.InterfaceConsts.Id} = ?", new[] { rawId.ToString() })
                                    .WithYieldAllowed(true)
                                    .Build());
                            }
                            resolver.ApplyBatch(ContactsContract.Authority, deleteOps);
                        }
                    }
                }
                else if (changes.DeletedIds != null && changes.DeletedIds.Count > 0)
                {
                    // Artımlı senkronda gelen silinenler
                    foreach (var chunk in changes.DeletedIds.Chunk(50))
                    {
                        var deleteOps = new List<ContentProviderOperation>();
                        foreach (var id in chunk)
                        {
                            deleteOps.Add(ContentProviderOperation.NewDelete(BuildSyncAdapterUri(ContactsContract.RawContacts.ContentUri!))
                                .WithSelection($"{ContactsContract.RawContacts.InterfaceConsts.AccountType} = ? AND {ContactsContract.RawContacts.InterfaceConsts.SourceId} = ?",
                                               new[] { AccountType, id.ToString() })
                                .WithYieldAllowed(true)
                                .Build());
                        }
                        resolver.ApplyBatch(ContactsContract.Authority, deleteOps);
                    }
                }

                // 2. Toplu Upsert (50 kişilik parçalar halinde)
                int totalCount = 0;
                foreach (var batch in changes.Upserted.Chunk(50))
                {
                    var ops = new List<ContentProviderOperation>();
                    foreach (var contact in batch)
                    {
                        bool hasDelete = deviceContacts.TryGetValue(contact.Id, out var existingRawId);
                        if (hasDelete)
                        {
                            ops.Add(ContentProviderOperation.NewDelete(BuildSyncAdapterUri(ContactsContract.RawContacts.ContentUri!))
                                .WithSelection($"{ContactsContract.RawContacts.InterfaceConsts.Id} = ?", new[] { existingRawId.ToString() })
                                .WithYieldAllowed(true)
                                .Build());
                        }

                        int rawIndex = ops.Count;
                        var rawInsert = ContentProviderOperation.NewInsert(BuildSyncAdapterUri(ContactsContract.RawContacts.ContentUri!))
                            .WithValue(ContactsContract.RawContacts.InterfaceConsts.AccountName, AccountName)
                            .WithValue(ContactsContract.RawContacts.InterfaceConsts.AccountType, AccountType)
                            .WithValue(ContactsContract.RawContacts.InterfaceConsts.SourceId, contact.Id.ToString())
                            .WithValue(ContactsContract.RawContacts.InterfaceConsts.Sync1, contact.Id.ToString());

                        if (!hasDelete)
                        {
                            rawInsert.WithYieldAllowed(true);
                        }

                        ops.Add(rawInsert.Build());

                        // İsim
                        ops.Add(ContentProviderOperation.NewInsert(BuildSyncAdapterUri(ContactsContract.Data.ContentUri!))
                            .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, rawIndex)
                            .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.StructuredName.ContentItemType)
                            .WithValue(ContactsContract.CommonDataKinds.StructuredName.DisplayName, contact.DisplayName)
                            .Build());

                        // Telefon
                        if (!string.IsNullOrWhiteSpace(contact.TelephoneNumber))
                        {
                            ops.Add(ContentProviderOperation.NewInsert(BuildSyncAdapterUri(ContactsContract.Data.ContentUri!))
                                .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, rawIndex)
                                .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Phone.ContentItemType)
                                .WithValue(ContactsContract.CommonDataKinds.Phone.Number, contact.TelephoneNumber)
                                .WithValue(ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type, (int)PhoneDataKind.Mobile)
                                .Build());
                        }

                        // E-posta
                        if (!string.IsNullOrWhiteSpace(contact.Email))
                        {
                            ops.Add(ContentProviderOperation.NewInsert(BuildSyncAdapterUri(ContactsContract.Data.ContentUri!))
                                .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, rawIndex)
                                .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Email.ContentItemType)
                                .WithValue(ContactsContract.CommonDataKinds.Email.Address, contact.Email)
                                .WithValue(ContactsContract.CommonDataKinds.Email.InterfaceConsts.Type, (int)EmailDataKind.Work)
                                .Build());
                        }

                        // Şirket & Unvan & Departman
                        if (!string.IsNullOrWhiteSpace(contact.Company) || !string.IsNullOrWhiteSpace(contact.JobTitle) || !string.IsNullOrWhiteSpace(contact.Department))
                        {
                            ops.Add(ContentProviderOperation.NewInsert(BuildSyncAdapterUri(ContactsContract.Data.ContentUri!))
                                .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, rawIndex)
                                .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Organization.ContentItemType)
                                .WithValue(ContactsContract.CommonDataKinds.Organization.Company, contact.Company ?? "AUNDE")
                                .WithValue(ContactsContract.CommonDataKinds.Organization.Title, contact.JobTitle ?? "")
                                .WithValue(ContactsContract.CommonDataKinds.Organization.Department, contact.Department ?? "")
                                .Build());
                        }

                        // Dahili / Not
                        if (!string.IsNullOrWhiteSpace(contact.Home))
                        {
                            ops.Add(ContentProviderOperation.NewInsert(BuildSyncAdapterUri(ContactsContract.Data.ContentUri!))
                                .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, rawIndex)
                                .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Note.ContentItemType)
                                .WithValue(ContactsContract.IDataColumns.Data1, $"Dahili / Kısa Kod: {contact.Home}")
                                .Build());
                        }

                        totalCount++;
                    }

                    resolver.ApplyBatch(ContactsContract.Authority, ops);
                }

                _settingsService.LastSyncTime = changes.ServerTimeUtc;
                _settingsService.LastSyncError = null;

                return totalCount;
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = $"Senkronizasyon hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"AndroidContactSyncService hatası: {ex}");
                return 0;
            }
            finally
            {
                _syncSemaphore.Release();
            }
        }

        private static Dictionary<int, long> GetDeviceStaffSyncContacts(ContentResolver resolver)
        {
            var map = new Dictionary<int, long>();
            var uri = BuildSyncAdapterUri(ContactsContract.RawContacts.ContentUri!);
            var projection = new[]
            {
                ContactsContract.RawContacts.InterfaceConsts.Id,
                ContactsContract.RawContacts.InterfaceConsts.SourceId
            };
            var selection = $"{ContactsContract.RawContacts.InterfaceConsts.AccountType} = ?";
            var selectionArgs = new[] { AccountType };

            using var cursor = resolver.Query(uri, projection, selection, selectionArgs, null);
            if (cursor != null)
            {
                int idCol = cursor.GetColumnIndex(ContactsContract.RawContacts.InterfaceConsts.Id);
                int sourceIdCol = cursor.GetColumnIndex(ContactsContract.RawContacts.InterfaceConsts.SourceId);
                while (cursor.MoveToNext())
                {
                    var rawId = cursor.GetLong(idCol);
                    var sourceIdStr = cursor.GetString(sourceIdCol);
                    if (int.TryParse(sourceIdStr, out var contactId))
                    {
                        map[contactId] = rawId;
                    }
                }
            }
            return map;
        }

        public async Task<int> GetSyncedContactsCountAsync(CancellationToken ct = default)
        {
            try
            {
                var readStatus = await Permissions.CheckStatusAsync<Permissions.ContactsRead>();
                if (readStatus != PermissionStatus.Granted) return 0;

                var context = global::Android.App.Application.Context;
                var resolver = context.ContentResolver;
                if (resolver == null) return 0;

                var deviceContacts = GetDeviceStaffSyncContacts(resolver);
                return deviceContacts.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSyncedContactsCountAsync Android hatası: {ex}");
                return 0;
            }
        }

        public Task ClearSyncedContactsAsync(CancellationToken ct = default)
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var account = new Account(AccountName, AccountType);
                var accountManager = AccountManager.Get(context);
                if (accountManager != null)
                {
                    try
                    {
                        if (OperatingSystem.IsAndroidVersionAtLeast(22))
                        {
                            accountManager.RemoveAccountExplicitly(account);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Hesap silme uyarısı: {ex.Message}");
                    }
                }

                var resolver = context.ContentResolver;
                if (resolver != null)
                {
                    var deleteUri = BuildSyncAdapterUri(ContactsContract.RawContacts.ContentUri!);
                    var where = $"{ContactsContract.RawContacts.InterfaceConsts.AccountType} = ?";
                    resolver.Delete(deleteUri, where, new[] { AccountType });
                }

                _settingsService.LastSyncError = null;
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = $"Kişiler temizlenirken hata: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"ClearSyncedContactsAsync hatası: {ex}");
            }

            _settingsService.LastSyncTime = null;
            return Task.CompletedTask;
        }
    }
}
#endif
