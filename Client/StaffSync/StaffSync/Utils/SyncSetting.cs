using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Utils
{
    public class SyncSetting
    {
        private const string SyncEnabledKey = "AundeStaffSync_enabled";
        private static readonly bool SyncEnabledDefault = false;

        public static bool IsSyncEnabled
        {
            get => Preferences.Default.Get(SyncEnabledKey, SyncEnabledDefault);
            set => Preferences.Default.Set(SyncEnabledKey, value);
        }
    }
}
