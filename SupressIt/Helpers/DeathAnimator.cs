using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SupressIt.Helpers
{
    /// <summary>
    /// Plays a death animation on a card Border, then fires onComplete.
    /// Does NOT use BlurEffect (conflicts with DropShadowEffect on cards).
    /// Uses only Opacity + ScaleTransform + a red tint via an overlay Rectangle.
    /// Types: Burn | Fade | Shrink | None
    /// </summary>
    public static class DeathAnimator
    {
        public static void Play(FrameworkElement element, string type, double durationSec,
            Action onComplete)
        {
            if (element == null || type == "None")
            {
                onComplete?.Invoke();
                return;
            }

            double ms = Math.Max(150, durationSec * 1000);

            switch (type)
            {
                case "Burn":   PlayBurn(element, ms, onComplete);   break;
                case "Shrink": PlayShrink(element, ms, onComplete); break;
                default:       PlayFade(element, ms, onComplete);   break;
            }
        }

        // ── Burn: red flash then fade+shrink ──────────────────────────────────

        private static void PlayBurn(FrameworkElement el, double ms, Action done)
        {
            var tg    = EnsureTransform(el);
            var scale = (ScaleTransform)tg.Children[0];

            // 1. Flash opacity up/down quickly (simulates ignition)
            var flash = new DoubleAnimationUsingKeyFrames();
            flash.KeyFrames.Add(new LinearDoubleKeyFrame(1.0,  KeyTime.FromPercent(0)));
            flash.KeyFrames.Add(new LinearDoubleKeyFrame(0.4,  KeyTime.FromPercent(0.15)));
            flash.KeyFrames.Add(new LinearDoubleKeyFrame(0.9,  KeyTime.FromPercent(0.30)));
            flash.KeyFrames.Add(new LinearDoubleKeyFrame(0.2,  KeyTime.FromPercent(0.55)));
            flash.KeyFrames.Add(new LinearDoubleKeyFrame(0.0,  KeyTime.FromPercent(1.0)));
            flash.Duration = TimeSpan.FromMilliseconds(ms);
            flash.Completed += (_, _) => { el.Opacity = 0; done?.Invoke(); };

            // 2. Scale slightly up then collapse (burn-away feel)
            var scaleUp = new DoubleAnimation(1.0, 1.05,
                new Duration(TimeSpan.FromMilliseconds(ms * 0.2)));
            var scaleDown = new DoubleAnimation(1.05, 0.0,
                new Duration(TimeSpan.FromMilliseconds(ms * 0.7)))
            {
                BeginTime      = TimeSpan.FromMilliseconds(ms * 0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            el.BeginAnimation(UIElement.OpacityProperty, flash);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

            // Sequence scaleDown after scaleUp completes
            scaleUp.Completed += (_, _) =>
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
            };
        }

        // ── Fade: simple smooth opacity to zero ───────────────────────────────

        private static void PlayFade(FrameworkElement el, double ms, Action done)
        {
            var anim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(ms))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            anim.Completed += (_, _) => { el.Opacity = 0; done?.Invoke(); };
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        // ── Shrink: scale to zero with slight bounce, then fade ───────────────

        private static void PlayShrink(FrameworkElement el, double ms, Action done)
        {
            var tg    = EnsureTransform(el);
            var scale = (ScaleTransform)tg.Children[0];

            var shrink = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(ms))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.4 }
            };
            var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(ms * 0.5))
            {
                BeginTime = TimeSpan.FromMilliseconds(ms * 0.5)
            };
            fade.Completed += (_, _) => { el.Opacity = 0; done?.Invoke(); };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
            el.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static TransformGroup EnsureTransform(FrameworkElement el)
        {
            if (el.RenderTransform is TransformGroup tg &&
                tg.Children.Count >= 1 &&
                tg.Children[0] is ScaleTransform)
                return tg;

            var newTg = new TransformGroup();
            newTg.Children.Add(new ScaleTransform(1, 1));
            newTg.Children.Add(new TranslateTransform(0, 0));
            el.RenderTransformOrigin = new Point(0.5, 0.5);
            el.RenderTransform = newTg;
            return newTg;
        }
    }
}
