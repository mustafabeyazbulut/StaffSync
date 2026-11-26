using StaffSync.Utils;

namespace StaffSync.Controls
{
    /// <summary>
    /// Yuvarlak kişi avatarı: fotoğraf varsa fotoğraf, yoksa baş harfler. Baş harf zemini kişiye göre
    /// sabit bir marka tonudur; böylece liste tek renk daire yığını gibi görünmez.
    /// </summary>
    public class ContactAvatar : ContentView
    {
        // Marka paleti: (zemin, yazı)
        private static readonly (Color Back, Color Text)[] Palette =
        {
            (Color.FromArgb("#233C82"), Colors.White),
            (Color.FromArgb("#1E325A"), Colors.White),
            (Color.FromArgb("#324155"), Colors.White),
            (Color.FromArgb("#737D91"), Colors.White),
            (Color.FromArgb("#A0B9EB"), Color.FromArgb("#1E2D48")),
        };

        public static readonly BindableProperty NameProperty = BindableProperty.Create(
            nameof(Name), typeof(string), typeof(ContactAvatar), null, propertyChanged: (b, _, _) => ((ContactAvatar)b).UpdateInitials());

        public static readonly BindableProperty PhotoPathProperty = BindableProperty.Create(
            nameof(PhotoPath), typeof(string), typeof(ContactAvatar), null, propertyChanged: (b, _, _) => ((ContactAvatar)b).UpdatePhoto());

        public static readonly BindableProperty SizeProperty = BindableProperty.Create(
            nameof(Size), typeof(double), typeof(ContactAvatar), 44d, propertyChanged: (b, _, _) => ((ContactAvatar)b).UpdateSize());

        public string? Name { get => (string?)GetValue(NameProperty); set => SetValue(NameProperty, value); }
        public string? PhotoPath { get => (string?)GetValue(PhotoPathProperty); set => SetValue(PhotoPathProperty, value); }
        public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

        private readonly Border _circle;
        private readonly Label _initials;
        private readonly Image _photo;

        public ContactAvatar()
        {
            _initials = new Label
            {
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            _photo = new Image { Aspect = Aspect.AspectFill, IsVisible = false };

            _circle = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = new Grid { Children = { _initials, _photo } },
            };
            Content = _circle;

            UpdateSize();
            UpdateInitials();
        }

        private void UpdateSize()
        {
            _circle.WidthRequest = _circle.HeightRequest = Size;
            _initials.FontSize = Math.Round(Size * 0.36);
        }

        private void UpdateInitials()
        {
            _initials.Text = DisplayText.Initials(Name);
            var (back, text) = Palette[StableIndex(Name, Palette.Length)];
            _circle.BackgroundColor = back;
            _initials.TextColor = text;
        }

        private void UpdatePhoto()
        {
            var hasPhoto = !string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath);
            _photo.Source = hasPhoto ? ImageSource.FromFile(PhotoPath) : null;
            _photo.IsVisible = hasPhoto;
            _initials.IsVisible = !hasPhoto;
        }

        /// <summary>string.GetHashCode her çalıştırmada değişir; renk her açılışta aynı kalsın diye sabit hesap.</summary>
        private static int StableIndex(string? text, int count)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            unchecked
            {
                int hash = 17;
                foreach (var ch in text)
                    hash = hash * 31 + ch;
                return (int)((uint)hash % (uint)count);
            }
        }
    }
}
