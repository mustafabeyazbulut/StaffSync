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

        /// <summary>
        /// Kullanıcı şirket hattı kullandığını bildirdi mi. Açıksa aramalar kısa kodla yapılır;
        /// kapalıysa (varsayılan) cep numarasıyla — kısa kod şirket hattı dışında çalışmaz.
        /// </summary>
        bool UsesCompanyLine { get; set; }

        /// <summary>Telefon rehberindeki kişilerin hangi biçimle yazıldığı (bkz. <see cref="ContactSyncFormat"/>).</summary>
        string? ContactFormat { get; set; }

        void ApplyTheme();
    }

    public class SettingsService : ISettingsService
    {
        private const string ThemeKey = "StaffSync_Theme";
        private const string SyncEnabledKey = "StaffSync_SyncEnabled";
        private const string LastSyncTimeKey = "StaffSync_LastSyncTime";
        private const string LastSyncErrorKey = "StaffSync_LastSyncError";
        private const string PermissionIntroKey = "StaffSync_PermissionIntro";
        private const string UsesCompanyLineKey = "StaffSync_UsesCompanyLine";
        private const string ContactFormatKey = "StaffSync_ContactFormat";

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

        public bool UsesCompanyLine
        {
            get => Preferences.Default.Get(UsesCompanyLineKey, false);
            set => Preferences.Default.Set(UsesCompanyLineKey, value);
        }

        public string? ContactFormat
        {
            get => Preferences.Default.Get(ContactFormatKey, (string?)null);
            set => Preferences.Default.Set(ContactFormatKey, value);
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
    }
}
