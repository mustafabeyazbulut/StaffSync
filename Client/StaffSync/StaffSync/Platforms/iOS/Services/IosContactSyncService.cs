#if IOS
using Contacts;
using Foundation;
using StaffSync.Models;
using StaffSync.Services;
using System.Text.Json;

namespace StaffSync.Platforms.iOS.Services
{
    public class IosContactSyncService : IContactSyncService
    {
        private const string GroupIdPrefKey = "Ios_StaffSync_GroupId";
        private static readonly string MapFilePath = Path.Combine(FileSystem.AppDataDirectory, "ios_contact_map.json");

        private static readonly NSString[] KeysToFetch = new[]
        {
            CNContactKey.GivenName,
            CNContactKey.FamilyName,
            CNContactKey.PhoneNumbers,
            CNContactKey.EmailAddresses,
            CNContactKey.OrganizationName,
            CNContactKey.DepartmentName,
            CNContactKey.JobTitle
        };

        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;

        public IosContactSyncService(IContactsService contactsService, ISettingsService settingsService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;
        }

        public bool IsSupported => true;

        private static Dictionary<int, string> LoadContactMap()
        {
            try
            {
                if (File.Exists(MapFilePath))
                {
                    var json = File.ReadAllText(MapFilePath);
                    var map = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
                    if (map != null) return map;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ios_contact_map okuma hatası: {ex}");
            }
            return new Dictionary<int, string>();
        }

        private static void SaveContactMap(Dictionary<int, string> map)
        {
            try
            {
                var json = JsonSerializer.Serialize(map);
                File.WriteAllText(MapFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ios_contact_map yazma hatası: {ex}");
            }
        }

        private static string EnsureGroup(CNContactStore store)
        {
            var existingGroupId = Preferences.Get(GroupIdPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(existingGroupId))
            {
                try
                {
                    var predicate = CNGroup.CreatePredicateForGroups(new[] { existingGroupId });
                    var groups = store.GetGroups(predicate, out var error);
                    if (error == null && groups != null && groups.Length > 0)
                    {
                        return existingGroupId;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Grup doğrulama hatası: {ex}");
                }
            }

            try
            {
                var groups = store.GetGroups(null, out var error);
                if (error == null && groups != null)
                {
                    var staffSyncGroup = groups.FirstOrDefault(g => g.Name == "StaffSync");
                    if (staffSyncGroup != null)
                    {
                        Preferences.Set(GroupIdPrefKey, staffSyncGroup.Identifier);
                        return staffSyncGroup.Identifier;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Grupları listeleme hatası: {ex}");
            }

            var newGroup = new CNMutableGroup { Name = "StaffSync" };
            var saveReq = new CNSaveRequest();
            saveReq.AddGroup(newGroup, null);
            if (store.ExecuteSaveRequest(saveReq, out var saveError) && saveError == null)
            {
                Preferences.Set(GroupIdPrefKey, newGroup.Identifier);
                return newGroup.Identifier;
            }

            return string.Empty;
        }

        private static (string GivenName, string FamilyName) SplitName(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return (string.Empty, string.Empty);

            var trimmed = displayName.Trim();
            var lastSpace = trimmed.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                var given = trimmed.Substring(0, lastSpace).Trim();
                var family = trimmed.Substring(lastSpace + 1).Trim();
                return (given, family);
            }
            return (trimmed, string.Empty);
        }

        public async Task<int> SyncContactsAsync(CancellationToken ct = default)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.ContactsWrite>();
                if (status != PermissionStatus.Granted)
                {
                    _settingsService.LastSyncError = "Rehber yazma izni verilmedi.";
                    return 0;
                }

                var lastSync = _settingsService.LastSyncTime;
                var changes = await _contactsService.GetChangesAsync(lastSync);
                if (changes == null)
                {
                    _settingsService.LastSyncError = "Sunucudan değişiklikler alınamadı.";
                    return 0;
                }

                using var store = new CNContactStore();
                var groupId = EnsureGroup(store);
                CNGroup? groupObj = null;
                if (!string.IsNullOrEmpty(groupId))
                {
                    var groups = store.GetGroups(CNGroup.CreatePredicateForGroups(new[] { groupId }), out _);
                    groupObj = groups?.FirstOrDefault();
                }

                var contactMap = LoadContactMap();

                // 1. Silme uzlaştırması
                if (!lastSync.HasValue)
                {
                    // Tam senkronda: cihaz eşlemesinde olup sunucuda olmayanları sil
                    var serverIds = new HashSet<int>(changes.Upserted.Select(c => c.Id));
                    var staleContactIds = contactMap.Keys.Where(id => !serverIds.Contains(id)).ToList();
                    if (staleContactIds.Count > 0)
                    {
                        var saveReq = new CNSaveRequest();
                        foreach (var staleId in staleContactIds)
                        {
                            if (contactMap.TryGetValue(staleId, out var identifier))
                            {
                                try
                                {
                                    var existing = store.GetUnifiedContact(identifier, KeysToFetch, out _);
                                    if (existing?.MutableCopy() is CNMutableContact mutable)
                                    {
                                        saveReq.DeleteContact(mutable);
                                    }
                                }
                                catch
                                {
                                    // Bulunamayan sessizce atlanır (spec)
                                }
                                contactMap.Remove(staleId);
                            }
                        }
                        store.ExecuteSaveRequest(saveReq, out _);
                    }
                }
                else if (changes.DeletedIds != null && changes.DeletedIds.Count > 0)
                {
                    var saveReq = new CNSaveRequest();
                    foreach (var deletedId in changes.DeletedIds)
                    {
                        if (contactMap.TryGetValue(deletedId, out var identifier))
                        {
                            try
                            {
                                var existing = store.GetUnifiedContact(identifier, KeysToFetch, out _);
                                if (existing?.MutableCopy() is CNMutableContact mutable)
                                {
                                    saveReq.DeleteContact(mutable);
                                }
                            }
                            catch
                            {
                                // Bulunamayan sessizce atlanır
                            }
                            contactMap.Remove(deletedId);
                        }
                    }
                    store.ExecuteSaveRequest(saveReq, out _);
                }

                // 2. Toplu Upsert (100'lük parçalar)
                int totalCount = 0;
                foreach (var chunk in changes.Upserted.Chunk(100))
                {
                    var saveRequest = new CNSaveRequest();
                    foreach (var contact in chunk)
                    {
                        CNContact? existingContact = null;
                        if (contactMap.TryGetValue(contact.Id, out var existingId) && !string.IsNullOrEmpty(existingId))
                        {
                            try
                            {
                                existingContact = store.GetUnifiedContact(existingId, KeysToFetch, out _);
                            }
                            catch
                            {
                                existingContact = null;
                            }
                        }

                        var (givenName, familyName) = SplitName(contact.DisplayName);

                        if (existingContact != null)
                        {
                            if (existingContact.MutableCopy() is CNMutableContact mutable)
                            {
                                mutable.GivenName = givenName;
                                mutable.FamilyName = familyName;
                                mutable.OrganizationName = contact.Company ?? "AUNDE";
                                mutable.DepartmentName = contact.Department ?? "";
                                mutable.JobTitle = contact.JobTitle ?? "";

                                if (!string.IsNullOrWhiteSpace(contact.TelephoneNumber))
                                {
                                    mutable.PhoneNumbers = new[]
                                    {
                                        new CNLabeledValue<CNPhoneNumber>(
                                            CNLabelPhoneNumberKey.Mobile,
                                            new CNPhoneNumber(contact.TelephoneNumber))
                                    };
                                }
                                else
                                {
                                    mutable.PhoneNumbers = Array.Empty<CNLabeledValue<CNPhoneNumber>>();
                                }

                                if (!string.IsNullOrWhiteSpace(contact.Email))
                                {
                                    mutable.EmailAddresses = new[]
                                    {
                                        new CNLabeledValue<NSString>(
                                            CNLabelKey.Work,
                                            new NSString(contact.Email))
                                    };
                                }
                                else
                                {
                                    mutable.EmailAddresses = Array.Empty<CNLabeledValue<NSString>>();
                                }

                                saveRequest.UpdateContact(mutable);
                            }
                        }
                        else
                        {
                            var newContact = new CNMutableContact
                            {
                                GivenName = givenName,
                                FamilyName = familyName,
                                OrganizationName = contact.Company ?? "AUNDE",
                                DepartmentName = contact.Department ?? "",
                                JobTitle = contact.JobTitle ?? ""
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

                            saveRequest.AddContact(newContact, null);
                            if (groupObj != null)
                            {
                                saveRequest.AddMember(newContact, groupObj);
                            }
                            contactMap[contact.Id] = newContact.Identifier;
                        }

                        totalCount++;
                    }

                    store.ExecuteSaveRequest(saveRequest, out var saveErr);
                    if (saveErr != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"CNSaveRequest hatası: {saveErr.LocalizedDescription}");
                    }
                }

                SaveContactMap(contactMap);

                _settingsService.LastSyncTime = changes.ServerTimeUtc;
                _settingsService.LastSyncError = null;

                return totalCount;
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = $"iOS senkron hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"IosContactSyncService hatası: {ex}");
                return 0;
            }
        }

        public Task ClearSyncedContactsAsync(CancellationToken ct = default)
        {
            try
            {
                using var store = new CNContactStore();
                var contactMap = LoadContactMap();

                if (contactMap.Count > 0)
                {
                    var saveReq = new CNSaveRequest();
                    foreach (var identifier in contactMap.Values)
                    {
                        try
                        {
                            var existing = store.GetUnifiedContact(identifier, KeysToFetch, out _);
                            if (existing?.MutableCopy() is CNMutableContact mutable)
                            {
                                saveReq.DeleteContact(mutable);
                            }
                        }
                        catch
                        {
                            // Bulunamayan sessizce atlanır (spec)
                        }
                    }
                    store.ExecuteSaveRequest(saveReq, out _);
                }

                // Grup silme
                var groupId = Preferences.Get(GroupIdPrefKey, string.Empty);
                if (!string.IsNullOrEmpty(groupId))
                {
                    try
                    {
                        var groups = store.GetGroups(CNGroup.CreatePredicateForGroups(new[] { groupId }), out _);
                        var groupObj = groups?.FirstOrDefault();
                        if (groupObj?.MutableCopy() is CNMutableGroup mutableGroup)
                        {
                            var groupDeleteReq = new CNSaveRequest();
                            groupDeleteReq.DeleteGroup(mutableGroup);
                            store.ExecuteSaveRequest(groupDeleteReq, out _);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Grup silme hatası: {ex}");
                    }
                    Preferences.Remove(GroupIdPrefKey);
                }

                // Eşleme dosyasını sil
                if (File.Exists(MapFilePath))
                {
                    try
                    {
                        File.Delete(MapFilePath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Eşleme dosyası silme hatası: {ex}");
                    }
                }

                _settingsService.LastSyncError = null;
            }
            catch (Exception ex)
            {
                _settingsService.LastSyncError = $"Kişiler temizlenirken hata: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"ClearSyncedContactsAsync iOS hatası: {ex}");
            }

            _settingsService.LastSyncTime = null;
            return Task.CompletedTask;
        }
    }
}
#endif
