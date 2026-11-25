#if ANDROID
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.OS;
using StaffSync.Services;

namespace StaffSync.Platforms.Android.Services
{
    [Service(Exported = true, Name = "com.aunde.staffsync.SyncService")]
    [IntentFilter(new[] { "android.content.SyncAdapter" })]
    [MetaData("android.content.SyncAdapter", Resource = "@xml/syncadapter")]
    public class SyncService : Service
    {
        private static SyncAdapter? _syncAdapter;
        private static readonly object _syncLock = new();

        public override void OnCreate()
        {
            base.OnCreate();
            lock (_syncLock)
            {
                _syncAdapter ??= new SyncAdapter(ApplicationContext, true);
            }
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return _syncAdapter?.SyncAdapterBinder;
        }
    }

    public class SyncAdapter : AbstractThreadedSyncAdapter
    {
        public SyncAdapter(Context? context, bool autoInitialize) : base(context, autoInitialize)
        {
        }

        public SyncAdapter(Context? context, bool autoInitialize, bool allowParallelSyncs) : base(context, autoInitialize, allowParallelSyncs)
        {
        }

        public override void OnPerformSync(Account? account, Bundle? extras, string? authority, ContentProviderClient? provider, SyncResult? syncResult)
        {
            try
            {
                var services = IPlatformApplication.Current?.Services;
                if (services != null)
                {
                    var syncService = services.GetService<IContactSyncService>();
                    syncService?.SyncContactsAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnPerformSync hatası: {ex}");
            }
        }
    }
}
#endif
