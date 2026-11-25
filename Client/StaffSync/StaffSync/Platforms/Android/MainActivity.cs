using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace StaffSync
{
    // Label: ana ekran/çekmecedeki ikon altı adı. "AUNDE Rehber" sığmayıp kesildiği için kısa ad;
    // uygulamanın tam adı (Ayarlar > Uygulamalar) manifestteki application label'da kalır.
    [Activity(Label = "Rehber", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
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

            // Durum çubuğu o an görünen sayfanın zeminiyle aynı renkte olsun (detay/profil gri, rehber beyaz)
            var statusBarColor = isDark ? Android.Graphics.Color.ParseColor("#0B1220") : Android.Graphics.Color.ParseColor("#FFFFFF");
            var pageBg = CurrentPage()?.BackgroundColor;
            if (pageBg != null && pageBg.Alpha > 0.99f)
            {
                statusBarColor = Android.Graphics.Color.Argb(255, (int)(pageBg.Red * 255), (int)(pageBg.Green * 255), (int)(pageBg.Blue * 255));
            }
            if (OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                // Android 15+ (edge-to-edge zorunlu): durum çubuğu her zaman saydam, arkasındaki
                // pencere zemini görünür; rengi zemine veriyoruz.
                Window.DecorView.SetBackgroundColor(statusBarColor);
            }
            else
            {
                Window.SetStatusBarColor(statusBarColor);
            }

            var windowInsetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (windowInsetsController != null)
            {
                windowInsetsController.AppearanceLightStatusBars = !isDark;
            }
        }

        static Microsoft.Maui.Controls.Page? CurrentPage()
        {
            var shell = Microsoft.Maui.Controls.Shell.Current;
            if (shell == null) return null;
            var modal = shell.Navigation?.ModalStack;
            if (modal != null && modal.Count > 0) return modal[^1];
            return shell.CurrentPage;
        }
    }
}
