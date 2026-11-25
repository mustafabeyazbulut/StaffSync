#if ANDROID
using Android.Accounts;
using Android.Content;
using Android.OS;
using Android.Provider;
using CommunityToolkit.Mvvm.Messaging;
using StaffSync.Messages;
using StaffSync.Models;
using StaffSync.Services;
using StaffSync.Utils;

namespace StaffSync.Platforms.Android.Services
{
    public class AndroidContactSyncService : IContactSyncService
    {
        public const string AccountName = "AUNDE Rehber";
        public const string AccountType = "com.aunde.staffsync";

        private static readonly SemaphoreSlim _syncSemaphore = new(1, 1);

        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;
        private readonly IContactPhotoService _photoService;

        public AndroidContactSyncService(IContactsService contactsService, ISettingsService settingsService, IContactPhotoService photoService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;
            _photoService = photoService;
        }

        /// <summary>Kişinin fotoğrafını rehberdeki kaydına yazar; fotoğraf inemezse kişi fotoğrafsız kalır.</summary>
        private async Task WritePhotoAsync(ContentResolver resolver, long rawContactId, ContactDto contact, CancellationToken ct)
        {
            try
            {
                var path = await _photoService.GetLocalPathAsync(contact, ct);
                if (path == null)
                    return;

                var bytes = await File.ReadAllBytesAsync(path, ct);
                var photoUri = global::Android.Net.Uri.WithAppendedPath(
                    ContentUris.WithAppendedId(ContactsContract.RawContacts.ContentUri!, rawContactId),
                    ContactsContract.RawContacts.DisplayPhoto.ContentDirectory)!;

                using var descriptor = resolver.OpenAssetFileDescriptor(photoUri, "rw");
                using var output = descriptor?.CreateOutputStream();
                if (output != null)
                    await output.WriteAsync(bytes, ct);
            }
            catch (Exception ex) when (ex is IOException || ex is Java.Lang.Exception)
            {
                System.Diagnostics.Debug.WriteLine($"Rehbere fotoğraf yazılamadı ({contact.Id}): {ex.Message}");
            }
        }

        public bool IsSupported => true;

        public bool IsSyncRunning => _syncSemaphore.CurrentCount == 0;

        private static global::Android.Net.Uri BuildSyncAdapterUri(global::Android.Net.Uri baseUri)
        {
            return baseUri.BuildUpon()!
                .AppendQueryParameter(ContactsContract.CallerIsSyncadapter, "true")!
                .AppendQueryParameter(ContactsContract.RawContacts.InterfaceConsts.AccountName, AccountName)!
                .AppendQueryParameter(ContactsContract.RawContacts.InterfaceConsts.AccountType, AccountType)!
                .Build()!;
        }

        /// <returns>
        /// Hesap yoktu ve yeni açıldıysa true. Bu durumda cihazda kişi yoktur (ör. kullanıcı hesabı
        /// Ayarlar'dan silmiş), artımlı değil tam senkron gerekir.
        /// </returns>
        public static bool EnsureAccount(Context context)
        {
            var account = new Account(AccountName, AccountType);
            var created = false;
            var accountManager = AccountManager.Get(context);
            if (accountManager != null)
            {
                var accounts = accountManager.GetAccountsByType(AccountType) ?? Array.Empty<Account>();
                if (!accounts.Any(a => a.Name == AccountName))
                {
                    created = accountManager.AddAccountExplicitly(account, null, null);
                }
            }

            ContentResolver.SetIsSyncable(account, ContactsContract.Authority, 1);
            ContentResolver.SetSyncAutomatically(account, ContactsContract.Authority, true);
            ContentResolver.AddPeriodicSync(account, ContactsContract.Authority, Bundle.Empty, 86400);

            EnsureContactsVisible(context);
            return created;
        }

        /// <summary>
        /// Android, özel hesaplarda gruba bağlı olmayan kişileri varsayılan olarak gizler
        /// (Settings.UNGROUPED_VISIBLE = 0). Bu ayar açılmazsa kişiler rehber veritabanına yazılır
        /// ama Rehber uygulamasında görünmez.
        /// </summary>
        private static void EnsureContactsVisible(Context context)
        {
            var resolver = context.ContentResolver;
            if (resolver == null)
                return;

            try
            {
                var uri = BuildSyncAdapterUri(ContactsContract.Settings.ContentUri!);
                var values = new ContentValues();
                values.Put(ContactsContract.ISettingsColumns.UngroupedVisible, 1);

                var where = $"{ContactsContract.ISettingsColumns.AccountName}=? AND {ContactsContract.ISettingsColumns.AccountType}=?";
                var updated = resolver.Update(uri, values, where, new[] { AccountName, AccountType });
                if (updated == 0)
                {
                    values.Put(ContactsContract.ISettingsColumns.AccountName, AccountName);
                    values.Put(ContactsContract.ISettingsColumns.AccountType, AccountType);
                    resolver.Insert(uri, values);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidContactSyncService: Görünürlük ayarı yazılamadı: {ex.Message}");
            }
        }

        public async Task<int> SyncContactsAsync(CancellationToken ct = default)
        {
            // Başka bir senkron sürüyorsa (açılış / SyncAdapter) atlamak yerine bitmesini bekle:
            // atlanırsa kullanıcı "0 kişi senkronize edildi" görüyordu. Ardından yapılan artımlı
            // senkron sadece aradaki değişiklikleri çeker, maliyeti düşük.
            await _syncSemaphore.WaitAsync(ct);
            WeakReferenceMessenger.Default.Send(new SyncStateChangedMessage(true));

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
                var accountCreated = EnsureAccount(context);

                var resolver = context.ContentResolver;
                if (resolver == null)
                {
                    _settingsService.LastSyncError = "ContentResolver bulunamadı.";
                    return 0;
                }

                // Hesap yeni açıldıysa cihazda kişi yoktur; kayıt biçimi değiştiyse kişiler yeniden
                // yazılmalı. İki durumda da artımlı değil tam senkron yap.
                var lastSync = accountCreated || ContactSyncFormat.IsOutdated(_settingsService) ? null : _settingsService.LastSyncTime;
                var writeShortCode = _settingsService.UsesCompanyLine;
                var changes = await _contactsService.GetChangesAsync(lastSync);
                if (changes == null)
                {
                    _settingsService.LastSyncError = "Sunucudan değişiklikler alınamadı.";
                    return 0;
                }

                var cacheService = IPlatformApplication.Current?.Services.GetService<IContactCacheService>();
                if (cacheService != null)
                {
                    await cacheService.RefreshCacheAsync(ct);
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
                var photoTargets = new List<(ContactDto Contact, int RawInsertIndex)>();
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

                        // Kısa kod: kullanıcı şirket hattındaysa asıl aranan numara. Varsayılan (super primary)
                        // yapılır ki rehberden kişiye dokununca kısa kod aransın. Şirket hattı değilse kısa kod
                        // çalışmaz, rehbere aranabilir numara olarak yazılmaz.
                        if (writeShortCode && contact.HasShortCode)
                        {
                            ops.Add(ContentProviderOperation.NewInsert(BuildSyncAdapterUri(ContactsContract.Data.ContentUri!))
                                .WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, rawIndex)
                                .WithValue(ContactsContract.Data.InterfaceConsts.Mimetype, ContactsContract.CommonDataKinds.Phone.ContentItemType)
                                .WithValue(ContactsContract.CommonDataKinds.Phone.Number, contact.Home!.Trim())
                                .WithValue(ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type, (int)PhoneDataKind.Custom)
                                .WithValue(ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Label, DisplayText.ShortCodeLabel)
                                .WithValue(ContactsContract.Data.InterfaceConsts.IsPrimary, 1)
                                .WithValue(ContactsContract.Data.InterfaceConsts.IsSuperPrimary, 1)
                                .Build());
                        }

                        // Cep: gelen aramada arayan kimliği bu numarayla eşleşir, o yüzden kısa kod olsa da yazılır
                        if (contact.HasPhone)
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

                        if (contact.HasPhotoUrl)
                            photoTargets.Add((contact, rawIndex));

                        totalCount++;
                    }

                    var results = resolver.ApplyBatch(ContactsContract.Authority, ops);

                    // Fotoğraflar toplu işleme konmaz (Android'in 1 MB işlem sınırını aşar); kişi eklendikten
                    // sonra tek tek yazılır, rehber uygulaması boyutu kendisi küçültür.
                    foreach (var (photoContact, index) in photoTargets)
                    {
                        if (results?[index]?.Uri is { } rawUri)
                            await WritePhotoAsync(resolver, ContentUris.ParseId(rawUri), photoContact, ct);
                    }
                    photoTargets.Clear();
                }

                _settingsService.LastSyncTime = changes.ServerTimeUtc;
                _settingsService.LastSyncError = null;
                ContactSyncFormat.MarkWritten(_settingsService);

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
                WeakReferenceMessenger.Default.Send(new SyncStateChangedMessage(false));
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
