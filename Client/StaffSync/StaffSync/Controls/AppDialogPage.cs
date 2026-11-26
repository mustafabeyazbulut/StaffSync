using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;
using StaffSync.Services;
using StaffSync.Utils;

namespace StaffSync.Controls
{
    /// <summary>
    /// Marka tasarımlı uyarı/onay kartı ve bildirim. Saydam modal sayfa olarak açılır;
    /// <see cref="IDialogService"/> üzerinden kullanılır.
    /// </summary>
    public class AppDialogPage : ContentPage
    {
        private const uint AnimationMs = 160;
        private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(2.4);

        private readonly TaskCompletionSource<bool> _result = new();
        private readonly View _surface;
        private readonly bool _isToast;
        private bool _closing;

        public Task<bool> Result => _result.Task;

        private AppDialogPage(View surface, bool isToast)
        {
            _surface = surface;
            _isToast = isToast;

            Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
            On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);

            // Kartta arka plan karartılır; bildirimde ekran görünür kalır
            BackgroundColor = isToast ? Colors.Transparent : Color.FromRgba(0, 0, 0, 0.45);
            Padding = 0;

            var root = new Grid { Children = { surface } };
            var backgroundTap = new TapGestureRecognizer();
            // Kartın dışına dokunmak: bildirimde kapatır, kartta bir şey yapmaz (yanlışlıkla kapanmasın)
            backgroundTap.Tapped += (_, _) => { if (_isToast) Close(false); };
            root.GestureRecognizers.Add(backgroundTap);
            Content = root;
        }

        // ---- Fabrika metotları ----

        public static AppDialogPage Toast(string message, DialogKind kind)
        {
            var (accent, _, _) = KindStyle(kind);
            var pill = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Padding = new Thickness(18, 14),
                Margin = new Thickness(20, 0, 20, 110),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                BackgroundColor = Color.FromArgb("#1E2D48"),
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.25f, Radius = 16, Offset = new Point(0, 6) },
                Content = new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = IconFor(kind), FontFamily = "Lucide", FontSize = 18, TextColor = accent == Brand ? Color.FromArgb("#A0B9EB") : accent, VerticalOptions = LayoutOptions.Center },
                        new Label { Text = message, FontSize = 15, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap, MaximumWidthRequest = 280 },
                    }
                }
            };
            var page = new AppDialogPage(pill, isToast: true);
            pill.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => page.Close(false)) });
            return page;
        }

        public static AppDialogPage Alert(string title, string message, DialogKind kind, string button)
        {
            AppDialogPage? page = null;
            var card = Card(kind, title, message,
                PrimaryButton(button, KindStyle(kind).Accent == Danger ? Danger : Brand, () => page!.Close(true)));
            page = new AppDialogPage(card, isToast: false);
            return page;
        }

        public static AppDialogPage Confirm(string title, string message, string accept, string cancel, bool destructive)
        {
            AppDialogPage? page = null;
            var card = Card(destructive ? DialogKind.Error : DialogKind.Info, title, message,
                PrimaryButton(accept, destructive ? Danger : Brand, () => page!.Close(true)),
                SecondaryButton(cancel, () => page!.Close(false)));
            page = new AppDialogPage(card, isToast: false);
            return page;
        }

        // ---- Yaşam döngüsü ----

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _surface.Opacity = 0;
            _surface.Scale = _isToast ? 1 : 0.94;
            _surface.TranslationY = _isToast ? 16 : 0;
            await Task.WhenAll(
                _surface.FadeToAsync(1, AnimationMs, Easing.CubicOut),
                _surface.ScaleToAsync(1, AnimationMs, Easing.CubicOut),
                _surface.TranslateToAsync(0, 0, AnimationMs, Easing.CubicOut));

            if (_isToast)
            {
                await Task.Delay(ToastDuration);
                Close(false);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Close(false); // geri tuşu = vazgeç
            return true;
        }

        private async void Close(bool result)
        {
            if (_closing)
                return;
            _closing = true;

            await _surface.FadeToAsync(0, AnimationMs / 2, Easing.CubicIn);
            await Navigation.PopModalAsync(animated: false);
            _result.TrySetResult(result);
        }

        // ---- Görünüm parçaları ----

        private static readonly Color Brand = Color.FromArgb("#233C82");
        private static readonly Color Danger = Color.FromArgb("#BE1937");
        private static readonly Color Success = Color.FromArgb("#1B7F4B");

        private static (Color Accent, Color SoftLight, Color SoftDark) KindStyle(DialogKind kind) => kind switch
        {
            DialogKind.Success => (Success, Color.FromArgb("#E3F4EA"), Color.FromArgb("#143524")),
            DialogKind.Error => (Danger, Color.FromArgb("#FDE7EA"), Color.FromArgb("#3A1620")),
            _ => (Brand, Color.FromArgb("#E1EBFF"), Color.FromArgb("#1A2A4D")),
        };

        private static string IconFor(DialogKind kind) => kind switch
        {
            DialogKind.Success => LucideIcons.CheckCircle,
            DialogKind.Error => LucideIcons.AlertTriangle,
            _ => LucideIcons.Info,
        };

        private static View Card(DialogKind kind, string title, string message, params View[] buttons)
        {
            var (accent, softLight, softDark) = KindStyle(kind);

            var icon = new Border
            {
                WidthRequest = 56,
                HeightRequest = 56,
                StrokeThickness = 0,
                StrokeShape = new Ellipse(),
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = IconFor(kind),
                    FontFamily = "Lucide",
                    FontSize = 26,
                    TextColor = accent,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
            };
            icon.SetAppThemeColor(Microsoft.Maui.Controls.VisualElement.BackgroundColorProperty, softLight, softDark);
            if (accent == Brand)
                ((Label)icon.Content).SetAppThemeColor(Label.TextColorProperty, Brand, Color.FromArgb("#8FB0FF"));

            var titleLabel = new Label
            {
                Text = title,
                FontSize = 19,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            titleLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#1E2D48"), Color.FromArgb("#EEF2F8"));

            var messageLabel = new Label
            {
                Text = message,
                FontSize = 15,
                LineHeight = 1.3,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            messageLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#5B667A"), Color.FromArgb("#9AA6BA"));

            var stack = new VerticalStackLayout { Spacing = 10, Children = { icon, titleLabel, messageLabel } };
            var buttonStack = new VerticalStackLayout { Spacing = 4, Margin = new Thickness(0, 12, 0, 0) };
            foreach (var b in buttons)
                buttonStack.Children.Add(b);
            stack.Children.Add(buttonStack);

            var card = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Padding = new Thickness(24, 28, 24, 16),
                Margin = new Thickness(28, 0),
                MaximumWidthRequest = 420,
                VerticalOptions = LayoutOptions.Center,
                Content = stack,
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.2f, Radius = 24, Offset = new Point(0, 8) },
            };
            card.SetAppThemeColor(Microsoft.Maui.Controls.VisualElement.BackgroundColorProperty, Colors.White, Color.FromArgb("#172033"));
            // Karta dokunmak arka plana geçmesin
            card.GestureRecognizers.Add(new TapGestureRecognizer());
            return card;
        }

        private static View PrimaryButton(string text, Color color, Action onTap)
        {
            var button = new Border
            {
                HeightRequest = 48,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                BackgroundColor = color,
                Content = new Label
                {
                    Text = text,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
            };
            Touch.SetFeedback(button, true);
            Touch.SetCornerRadius(button, 24);
            button.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { Haptics.Click(); onTap(); }) });
            return button;
        }

        private static View SecondaryButton(string text, Action onTap)
        {
            var label = new Label
            {
                Text = text,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#5B667A"), Color.FromArgb("#9AA6BA"));

            var button = new Border { HeightRequest = 44, StrokeThickness = 0, BackgroundColor = Colors.Transparent, Content = label, StrokeShape = new RoundRectangle { CornerRadius = 22 } };
            Touch.SetFeedback(button, true);
            Touch.SetCornerRadius(button, 22);
            button.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(onTap) });
            return button;
        }
    }
}
