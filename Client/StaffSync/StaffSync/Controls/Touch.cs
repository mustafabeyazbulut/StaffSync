namespace StaffSync.Controls
{
    /// <summary>
    /// TapGestureRecognizer kullanan görünümlere basılma geri bildirimi ekler.
    /// Android: yerel ripple dalgası (köşe yarıçapına göre maskelenir) + isteğe bağlı küçülme.
    /// iOS: basılıyken saydamlaşma + isteğe bağlı küçülme.
    /// Kullanım: controls:Touch.Feedback="True" controls:Touch.CornerRadius="22" controls:Touch.PressedScale="0.94"
    /// </summary>
    public static class Touch
    {
        public static readonly BindableProperty FeedbackProperty =
            BindableProperty.CreateAttached("Feedback", typeof(bool), typeof(Touch), false,
                propertyChanged: OnFeedbackChanged);

        /// <summary>
        /// Ripple maskesinin köşe yarıçapı (dp). Verilmezse (-1) otomatik: görünüm yuvarlak köşeli
        /// bir kartın içindeyse ve kartın üst/alt kenarına değiyorsa o köşeler kartın yarıçapını alır,
        /// böylece basılma izi kartın köşesinden taşmaz.
        /// </summary>
        public static readonly BindableProperty CornerRadiusProperty =
            BindableProperty.CreateAttached("CornerRadius", typeof(double), typeof(Touch), -1d);

        /// <summary>Basılıyken ölçek. 1 = küçülme yok (liste satırları için).</summary>
        public static readonly BindableProperty PressedScaleProperty =
            BindableProperty.CreateAttached("PressedScale", typeof(double), typeof(Touch), 1d);

        public static bool GetFeedback(BindableObject view) => (bool)view.GetValue(FeedbackProperty);
        public static void SetFeedback(BindableObject view, bool value) => view.SetValue(FeedbackProperty, value);
        public static double GetCornerRadius(BindableObject view) => (double)view.GetValue(CornerRadiusProperty);
        public static void SetCornerRadius(BindableObject view, double value) => view.SetValue(CornerRadiusProperty, value);
        public static double GetPressedScale(BindableObject view) => (double)view.GetValue(PressedScaleProperty);
        public static void SetPressedScale(BindableObject view, double value) => view.SetValue(PressedScaleProperty, value);

        static void OnFeedbackChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not VisualElement element)
                return;

            element.HandlerChanged -= OnHandlerChanged;
            if ((bool)newValue)
            {
                element.HandlerChanged += OnHandlerChanged;
                if (element.Handler != null)
                    Attach(element);
            }
        }

        static void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is VisualElement element && element.Handler != null)
                Attach(element);
        }

#if ANDROID
        static void Attach(VisualElement element)
        {
            if (element.Handler?.PlatformView is not Android.Views.View view)
                return;
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
                return;
            if (view.GetTag(Resource.Id.touch_feedback_tag) != null)
                return; // aynı yerel görünüme ikinci kez bağlanma (CollectionView geri dönüşümü)

            var mask = new Android.Graphics.Drawables.GradientDrawable();
            mask.SetColor(Android.Graphics.Color.White);

            view.Foreground = new Android.Graphics.Drawables.RippleDrawable(
                Android.Content.Res.ColorStateList.ValueOf(RippleColor()), null, mask);

            var tracker = new AndroidPressTracker(view, element, mask);
            view.Touch += tracker.OnTouch;
            view.SetTag(Resource.Id.touch_feedback_tag, Java.Lang.Boolean.True);
        }

        static Android.Graphics.Color RippleColor()
        {
            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
            return dark
                ? Android.Graphics.Color.Argb(0x4D, 0xFF, 0xFF, 0xFF)
                : Android.Graphics.Color.Argb(0x4D, 0x23, 0x3C, 0x82);
        }

        /// <summary>
        /// Android'in kendi davranışını taklit eder: kaydırma başlarsa basılı görünmesin diye
        /// basılı hali kısa bir gecikmeyle açar; çok hızlı dokunuşta da dalga görünsün diye
        /// bırakmayı biraz geciktirir.
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("android23.0")]
        sealed class AndroidPressTracker
        {
            const int PressDelayMs = 70;
            const int ReleaseDelayMs = 110;

            readonly Android.Views.View _view;
            readonly VisualElement _element;
            readonly Android.Graphics.Drawables.GradientDrawable _mask;
            Action? _pendingPress;
            Action? _pendingRelease;
            bool _pressed;

            public AndroidPressTracker(Android.Views.View view, VisualElement element, Android.Graphics.Drawables.GradientDrawable mask)
            {
                _view = view;
                _element = element;
                _mask = mask;
            }

            public void OnTouch(object? sender, Android.Views.View.TouchEventArgs e)
            {
                // e.Handled'a dokunulmaz: MAUI'nin kendi dokunma dinleyicisi karar verir.
                var ev = e.Event;
                if (ev == null)
                    return;

                switch (ev.ActionMasked)
                {
                    case Android.Views.MotionEventActions.Down:
                        CancelPending();
                        var x = ev.GetX();
                        var y = ev.GetY();
                        _pendingPress = () => Press(x, y);
                        _view.PostDelayed(_pendingPress, PressDelayMs);
                        break;

                    case Android.Views.MotionEventActions.Up:
                        if (_pendingPress != null)
                        {
                            CancelPending();
                            Press(ev.GetX(), ev.GetY());
                        }
                        _pendingRelease = Release;
                        _view.PostDelayed(_pendingRelease, ReleaseDelayMs);
                        break;

                    case Android.Views.MotionEventActions.Cancel:
                        CancelPending();
                        Release();
                        break;

                    case Android.Views.MotionEventActions.Move:
                        // Parmak görünümün dışına çıkarsa bırak
                        if (_pressed && (ev.GetX() < 0 || ev.GetY() < 0 || ev.GetX() > _view.Width || ev.GetY() > _view.Height))
                        {
                            CancelPending();
                            Release();
                        }
                        break;
                }
            }

            void Press(float x, float y)
            {
                _pendingPress = null;
                _pressed = true;
                PrepareRipple();
                _view.Foreground?.SetHotspot(x, y);
                _view.Pressed = true;

                var scale = (float)GetPressedScale(_element);
                if (scale < 1f)
                    _view.Animate()?.ScaleX(scale).ScaleY(scale).SetDuration(90).Start();
            }

            // Renk ve maske basış anında hesaplanır: tema değişmiş ya da satır listede yer değiştirmiş olabilir
            void PrepareRipple()
            {
                if (_view.Foreground is not Android.Graphics.Drawables.RippleDrawable ripple)
                    return;

                ripple.SetColor(Android.Content.Res.ColorStateList.ValueOf(RippleColor()));
                var mask = _mask;

                var density = _view.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
                var explicitRadius = GetCornerRadius(_element);
                if (explicitRadius >= 0)
                {
                    mask.SetCornerRadius((float)(explicitRadius * density));
                    return;
                }

                // Otomatik: en yakın yuvarlak köşeli Border'ı bul
                Element? parent = _element.Parent;
                Border? card = null;
                for (int i = 0; parent != null && i < 8; i++, parent = parent.Parent)
                {
                    if (parent is Page) break;
                    if (parent is Border b && b.StrokeShape is Microsoft.Maui.Controls.Shapes.RoundRectangle)
                    {
                        card = b;
                        break;
                    }
                }

                var radii = new float[8];
                if (card?.StrokeShape is Microsoft.Maui.Controls.Shapes.RoundRectangle rr
                    && card.Handler?.PlatformView is Android.Views.View cardView)
                {
                    var me = new int[2];
                    var other = new int[2];
                    _view.GetLocationInWindow(me);
                    cardView.GetLocationInWindow(other);
                    const int tolerance = 3;
                    var touchesTop = Math.Abs(me[1] - other[1]) <= tolerance;
                    var touchesBottom = Math.Abs(me[1] + _view.Height - (other[1] + cardView.Height)) <= tolerance;
                    var cr = rr.CornerRadius;
                    if (touchesTop)
                    {
                        radii[0] = radii[1] = (float)(cr.TopLeft * density);
                        radii[2] = radii[3] = (float)(cr.TopRight * density);
                    }
                    if (touchesBottom)
                    {
                        radii[4] = radii[5] = (float)(cr.BottomRight * density);
                        radii[6] = radii[7] = (float)(cr.BottomLeft * density);
                    }
                }
                mask.SetCornerRadii(radii);
            }

            void Release()
            {
                _pendingRelease = null;
                if (!_pressed)
                    return;
                _pressed = false;
                _view.Pressed = false;

                if (GetPressedScale(_element) < 1d)
                    _view.Animate()?.ScaleX(1f).ScaleY(1f).SetDuration(140).Start();
            }

            void CancelPending()
            {
                if (_pendingPress != null) { _view.RemoveCallbacks(_pendingPress); _pendingPress = null; }
                if (_pendingRelease != null) { _view.RemoveCallbacks(_pendingRelease); _pendingRelease = null; }
            }
        }
#elif IOS || MACCATALYST
        static void Attach(VisualElement element)
        {
            if (element.Handler?.PlatformView is not UIKit.UIView view)
                return;
            if (view.GestureRecognizers?.Any(g => g is PressGestureRecognizer) == true)
                return;

            view.AddGestureRecognizer(new PressGestureRecognizer(view, element));
        }

        sealed class PressGestureRecognizer : UIKit.UILongPressGestureRecognizer
        {
            public PressGestureRecognizer(UIKit.UIView view, VisualElement element)
            {
                MinimumPressDuration = 0.05;
                CancelsTouchesInView = false;
                ShouldRecognizeSimultaneously = (_, _) => true;

                AddTarget(() =>
                {
                    var scale = (nfloat)GetPressedScale(element);
                    switch (State)
                    {
                        case UIKit.UIGestureRecognizerState.Began:
                            UIKit.UIView.Animate(0.08, () =>
                            {
                                view.Alpha = 0.55f;
                                if (scale < 1) view.Transform = CoreGraphics.CGAffineTransform.MakeScale(scale, scale);
                            });
                            break;
                        case UIKit.UIGestureRecognizerState.Ended:
                        case UIKit.UIGestureRecognizerState.Cancelled:
                        case UIKit.UIGestureRecognizerState.Failed:
                            UIKit.UIView.Animate(0.18, () =>
                            {
                                view.Alpha = 1f;
                                view.Transform = CoreGraphics.CGAffineTransform.MakeIdentity();
                            });
                            break;
                    }
                });
            }
        }
#else
        static void Attach(VisualElement element) { }
#endif
    }
}
