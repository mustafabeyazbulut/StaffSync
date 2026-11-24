using StaffSync.Models;
using System.Text.Json;

namespace StaffSync.Services
{
    public enum ThemeOption
    {
        System = 0,
        Light = 1,
        Dark = 2
    }

    public interface ISettingsService
    {
        ThemeOption SelectedTheme { get; set; }
        bool IsSyncEnabled { get; set; }
        DateTime? LastSyncTime { get; set; }
        string? LastSyncError { get; set; }
        bool HasCompletedPermissionIntro { get; set; }

        void ApplyTheme();
        Task SaveCachedContactsAsync(List<ContactDto> contacts);
        Task<List<ContactDto>> GetCachedContactsAsync();
        DateTime? GetCachedContactsTimestamp();
    }

    public class SettingsService : ISettingsService
    {
        private const string ThemeKey = "StaffSync_Theme";
        private const string SyncEnabledKey = "StaffSync_SyncEnabled";
        private const string LastSyncTimeKey = "StaffSync_LastSyncTime";
        private const string LastSyncErrorKey = "StaffSync_LastSyncError";
        private const string PermissionIntroKey = "StaffSync_PermissionIntro";
        private const string CacheTimestampKey = "StaffSync_CacheTimestamp";
        private const string CacheFileName = "contacts_cache.json";

        private readonly string _cacheFilePath = Path.Combine(FileSystem.AppDataDirectory, CacheFileName);

        public ThemeOption SelectedTheme
        {
            get => (ThemeOption)Preferences.Default.Get(ThemeKey, (int)ThemeOption.System);
            set
            {
                Preferences.Default.Set(ThemeKey, (int)value);
                ApplyTheme();
            }
        }

        public bool IsSyncEnabled
        {
            get => Preferences.Default.Get(SyncEnabledKey, false);
            set => Preferences.Default.Set(SyncEnabledKey, value);
        }

        public DateTime? LastSyncTime
        {
            get
            {
                var val = Preferences.Default.Get(LastSyncTimeKey, string.Empty);
                if (DateTime.TryParse(val, out var dt))
                    return dt;
                return null;
            }
            set
            {
                if (value.HasValue)
                    Preferences.Default.Set(LastSyncTimeKey, value.Value.ToString("O"));
                else
                    Preferences.Default.Remove(LastSyncTimeKey);
            }
        }

        public string? LastSyncError
        {
            get => Preferences.Default.Get(LastSyncErrorKey, (string?)null);
            set
            {
                if (string.IsNullOrEmpty(value))
                    Preferences.Default.Remove(LastSyncErrorKey);
                else
                    Preferences.Default.Set(LastSyncErrorKey, value);
            }
        }

        public bool HasCompletedPermissionIntro
        {
            get => Preferences.Default.Get(PermissionIntroKey, false);
            set => Preferences.Default.Set(PermissionIntroKey, value);
        }

        public void ApplyTheme()
        {
            if (Application.Current is null)
                return;

            Application.Current.UserAppTheme = SelectedTheme switch
            {
                ThemeOption.Light => AppTheme.Light,
                ThemeOption.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }

        public async Task SaveCachedContactsAsync(List<ContactDto> contacts)
        {
            try
            {
                var json = JsonSerializer.Serialize(contacts);
                await File.WriteAllTextAsync(_cacheFilePath, json);
                Preferences.Default.Set(CacheTimestampKey, DateTime.UtcNow.ToString("O"));
            }
            catch
            {
                // Önbellek yazma hatası sessizce tolere edilir
            }
        }

        public async Task<List<ContactDto>> GetCachedContactsAsync()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                    return new List<ContactDto>();

                var json = await File.ReadAllTextAsync(_cacheFilePath);
                return JsonSerializer.Deserialize<List<ContactDto>>(json) ?? new List<ContactDto>();
            }
            catch
            {
                return new List<ContactDto>();
            }
        }

        public DateTime? GetCachedContactsTimestamp()
        {
            var val = Preferences.Default.Get(CacheTimestampKey, string.Empty);
            if (DateTime.TryParse(val, out var dt))
                return dt;
            return null;
        }
    }
}
