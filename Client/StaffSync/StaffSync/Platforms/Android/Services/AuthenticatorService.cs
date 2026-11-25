#if ANDROID
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.OS;

namespace StaffSync.Platforms.Android.Services
{
    [Service(Exported = true, Name = "com.aunde.staffsync.AuthenticatorService")]
    [IntentFilter(new[] { "android.accounts.AccountAuthenticator" })]
    [MetaData("android.accounts.AccountAuthenticator", Resource = "@xml/authenticator")]
    public class AuthenticatorService : Service
    {
        private Authenticator? _authenticator;

        public override void OnCreate()
        {
            base.OnCreate();
            _authenticator = new Authenticator(this);
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return _authenticator?.IBinder;
        }
    }

    public class Authenticator : AbstractAccountAuthenticator
    {
        public Authenticator(Context context) : base(context)
        {
        }

        public override Bundle? AddAccount(AccountAuthenticatorResponse? response, string? accountType, string? authTokenType, string[]? requiredFeatures, Bundle? options) => null;
        public override Bundle? ConfirmCredentials(AccountAuthenticatorResponse? response, Account? account, Bundle? options) => null;
        public override Bundle? EditProperties(AccountAuthenticatorResponse? response, string? accountType) => null;
        public override Bundle? GetAuthToken(AccountAuthenticatorResponse? response, Account? account, string? authTokenType, Bundle? options) => null;
        public override string? GetAuthTokenLabel(string? authTokenType) => null;
        public override Bundle? HasFeatures(AccountAuthenticatorResponse? response, Account? account, string[]? features) => null;
        public override Bundle? UpdateCredentials(AccountAuthenticatorResponse? response, Account? account, string? authTokenType, Bundle? options) => null;
    }
}
#endif
