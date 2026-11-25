using StaffSync.Utils;
using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ContactsPage : ContentPage
    {
        private readonly ContactsViewModel _viewModel;

        public ContactsPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<ContactsViewModel>()!)
        {
        }

        public ContactsPage(ContactsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            SkeletonList.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IsVisible))
                    UpdateSkeletonPulse();
            };
            UpdateSkeletonPulse();
        }

        // İskelet satırlar yavaşça nefes alır gibi soluklaşıp koyulaşır
        private void UpdateSkeletonPulse()
        {
            SkeletonList.AbortAnimation("SkeletonPulse");
            if (!SkeletonList.IsVisible)
            {
                SkeletonList.Opacity = 1;
                return;
            }

            var pulse = new Animation
            {
                { 0, 0.5, new Animation(v => SkeletonList.Opacity = v, 1, 0.45, Easing.SinInOut) },
                { 0.5, 1, new Animation(v => SkeletonList.Opacity = v, 0.45, 1, Easing.SinInOut) }
            };
            pulse.Commit(SkeletonList, "SkeletonPulse", length: 1100, repeat: () => SkeletonList.IsVisible);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }

        private CancellationTokenSource? _bubbleCts;

        private void OnLetterTapped(object? sender, EventArgs e)
        {
            if (sender is BindableObject bindable && bindable.BindingContext is string letter)
            {
                Haptics.Click();

                var group = _viewModel.GroupedContacts.FirstOrDefault(g => g.Key.Equals(letter, StringComparison.OrdinalIgnoreCase));
                if (group != null)
                {
                    GroupedListView.ScrollTo(group, position: ScrollToPosition.Start, animate: false);
                }

                _ = ShowLetterBubbleAsync(letter, hasGroup: group != null);
            }
        }

        private async Task ShowLetterBubbleAsync(string letter, bool hasGroup)
        {
            _bubbleCts?.Cancel();
            var cts = _bubbleCts = new CancellationTokenSource();

            LetterBubbleLabel.Text = letter;
            // O harfte kimse yoksa harf soluk görünür: dokunuş algılandı ama gidilecek yer yok
            LetterBubbleLabel.Opacity = hasGroup ? 1 : 0.5;
            const double targetOpacity = 0.94;

            LetterBubble.AbortAnimation("FadeTo");
            LetterBubble.AbortAnimation("ScaleTo");
            await Task.WhenAll(
                LetterBubble.FadeToAsync(targetOpacity, 90, Easing.CubicOut),
                LetterBubble.ScaleToAsync(1, 120, Easing.CubicOut));

            try { await Task.Delay(550, cts.Token); }
            catch (TaskCanceledException) { return; }

            await Task.WhenAll(
                LetterBubble.FadeToAsync(0, 200, Easing.CubicIn),
                LetterBubble.ScaleToAsync(0.8, 200, Easing.CubicIn));
        }
    }
}
