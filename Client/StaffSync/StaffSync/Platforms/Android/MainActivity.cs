using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace StaffSync
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            UpdateStatusBar();
        }

        protected override void OnResume()
        {
            base.OnResume();
            UpdateStatusBar();
        }

        public void UpdateStatusBar()
        {
            if (Window == null) return;

            var currentTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme;
            var isDark = currentTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
            if (currentTheme == Microsoft.Maui.ApplicationModel.AppTheme.Unspecified)
            {
                isDark = (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
            }

            var statusBarColor = isDark ? Android.Graphics.Color.ParseColor("#0B1220") : Android.Graphics.Color.ParseColor("#FFFFFF");
            Window.SetStatusBarColor(statusBarColor);

            var windowInsetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (windowInsetsController != null)
            {
                windowInsetsController.AppearanceLightStatusBars = !isDark;
            }
        }
    }
}
