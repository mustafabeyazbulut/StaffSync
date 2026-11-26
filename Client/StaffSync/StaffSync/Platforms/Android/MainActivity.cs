using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace StaffSync
{
    [Activity(Label = "StaffSync", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
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

        /// <summary>
        /// Durum çubuğunu o an görünen sayfanın zeminine boyar. Modal sayfalar Android'de ayrı bir
        /// diyalog penceresinde açıldığı için o pencereler de boyanır; aksi halde temanın beyazı kalır.
        /// </summary>
        public void UpdateStatusBar()
        {
            if (Window == null) return;

            var (opaque, visible) = StatusBarColors();
            var lightIcons = Luminance(visible) <= 0.5;

            // Ana pencere: en üstteki opak sayfanın zemini
            ApplyStatusBar(Window, opaque, lightIcons, isDialog: false);

            // Modal pencereler görünen rengi alır: opak modalda (filtre, departman) kendi zemini, uyarı
            // kartında altındaki sayfanın karartılmış hali (kartın karartması durum çubuğuna inmez).
            foreach (var fragment in SupportFragmentManager.Fragments)
            {
                if (fragment is AndroidX.Fragment.App.DialogFragment { Dialog.Window: { } dialogWindow })
                {
                    ApplyStatusBar(dialogWindow, visible, lightIcons, isDialog: true);
                }
            }
        }

        static void ApplyStatusBar(Android.Views.Window window, Android.Graphics.Color color, bool lightIcons, bool isDialog)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                // Android 15+ (edge-to-edge zorunlu): durum çubuğu her zaman saydam, arkasındaki
                // pencere zemini görünür; rengi ana pencerenin zeminine veriyoruz. Modal penceresinin
                // zemini boyanmaz, yoksa uyarı kartının arkasındaki ekran tamamen kapanır.
                if (!isDialog) window.DecorView.SetBackgroundColor(color);
            }
            else
            {
                window.SetStatusBarColor(color);
            }

            var insets = WindowCompat.GetInsetsController(window, window.DecorView);
            if (insets != null)
            {
                insets.AppearanceLightStatusBars = !lightIcons;
            }
        }

        static double Luminance(Android.Graphics.Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255;

        /// <summary>
        /// Opak: ana sayfa ve opak modallar (filtre, departman) içinde en üstteki zemin.
        /// Görünen: bunun üstüne yarı saydam modalların (uyarı kartı karartması) bindirilmiş hali;
        /// simgelerin açık/koyu seçimi buna göre yapılır.
        /// </summary>
        (Android.Graphics.Color Opaque, Android.Graphics.Color Visible) StatusBarColors()
        {
            var currentTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme;
            var isDark = currentTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
            if (currentTheme == Microsoft.Maui.ApplicationModel.AppTheme.Unspecified)
            {
                isDark = (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
            }

            var shell = Microsoft.Maui.Controls.Shell.Current;
            var (r, g, b) = isDark ? (0x0B / 255f, 0x12 / 255f, 0x20 / 255f) : (1f, 1f, 1f);
            var layers = new List<Microsoft.Maui.Controls.Page?> { BasePage(shell) };
            if (shell?.Navigation?.ModalStack is { } modals) layers.AddRange(modals);

            var opaque = (r, g, b);
            foreach (var bg in layers.Select(p => p?.BackgroundColor))
            {
                if (bg == null || bg.Alpha <= 0.01f) continue;
                var a = Math.Min(bg.Alpha, 1f);
                r = bg.Red * a + r * (1 - a);
                g = bg.Green * a + g * (1 - a);
                b = bg.Blue * a + b * (1 - a);
                if (a > 0.99f) opaque = (r, g, b);
            }

            return (ToColor(opaque), ToColor((r, g, b)));
        }

        /// <summary>
        /// Modalların altındaki sayfa. Shell.CurrentPage modal açıkken modalın kendisini döndürdüğü için
        /// kullanılmaz; o anki sekmenin yığınının tepesi (ya da sekmenin kök sayfası) alınır.
        /// </summary>
        static Microsoft.Maui.Controls.Page? BasePage(Microsoft.Maui.Controls.Shell? shell)
        {
            var section = shell?.CurrentItem?.CurrentItem;
            var pushed = section?.Navigation?.NavigationStack?.LastOrDefault(p => p != null);
            return pushed ?? (section?.CurrentItem as Microsoft.Maui.Controls.IShellContentController)?.Page;
        }

        static Android.Graphics.Color ToColor((float R, float G, float B) c) =>
            Android.Graphics.Color.Argb(255, (int)Math.Round(c.R * 255), (int)Math.Round(c.G * 255), (int)Math.Round(c.B * 255));
    }
}
