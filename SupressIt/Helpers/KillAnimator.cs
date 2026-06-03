using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using AnimatedImage.Wpf;
using Microsoft.Web.WebView2.Wpf;

namespace SupressIt.Helpers
{
    public sealed class KillAnimator
    {
        private readonly Canvas _overlay;
        private bool _busy;

        public KillAnimator(Canvas overlay)
        {
            _overlay = overlay;
        }

        public void Fly(
            string mediaPath,
            Point startScreen,
            Point targetScreen,
            double mediaSpeed,
            double flightSpeed,
            string animType,
            Action? onImpact = null,
            Action? onComplete = null)
        {
            if (_busy || string.IsNullOrWhiteSpace(mediaPath) || !File.Exists(mediaPath))
            {
                onImpact?.Invoke();
                return;
            }

            var flyer = CreateFlyer(mediaPath, mediaSpeed);
            if (flyer == null)
            {
                onImpact?.Invoke();
                return;
            }

            Point start;
            Point target;
            try
            {
                start = _overlay.PointFromScreen(startScreen);
                target = _overlay.PointFromScreen(targetScreen);
            }
            catch
            {
                onImpact?.Invoke();
                return;
            }

            var impactFired = false;
            var completeFired = false;
            void TriggerImpact()
            {
                if (impactFired)
                    return;

                impactFired = true;
                onImpact?.Invoke();
            }

            void TriggerComplete()
            {
                if (completeFired)
                    return;

                completeFired = true;
                onComplete?.Invoke();
            }

            void FailFlyer(FrameworkElement failedFlyer)
            {
                if (!_overlay.Children.Contains(failedFlyer))
                    return;

                TriggerImpact();
                RemoveFlyer(failedFlyer);
                _busy = false;
                TriggerComplete();
            }

            _busy = true;
            _overlay.Children.Add(flyer);

            if (flyer is WebView2 webVideo)
            {
                StartBrowserVideo(webVideo, mediaPath, mediaSpeed, () => FailFlyer(webVideo));
            }
            else if (flyer is MediaElement nativeVideo)
            {
                nativeVideo.MediaFailed += (_, _) => FailFlyer(nativeVideo);
                StartNativeVideo(nativeVideo);
            }

            var halfWidth = flyer.Width / 2;
            var halfHeight = flyer.Height / 2;

            Canvas.SetLeft(flyer, start.X - halfWidth);
            Canvas.SetTop(flyer, start.Y - halfHeight);
            flyer.Opacity = 1;

            var transform = (TransformGroup)flyer.RenderTransform;
            var scale = (ScaleTransform)transform.Children[0];
            var rotate = (RotateTransform)transform.Children[1];

            var safeFlightSpeed = Math.Max(0.1, flightSpeed);
            var flyMs = 380 / safeFlightSpeed;
            var pauseMs = 180;
            var returnMs = 300 / safeFlightSpeed;
            var fadeMs = Math.Min(200, returnMs);

            var flyEase = animType switch
            {
                "Bounce" => (IEasingFunction)new ElasticEase { Oscillations = 1, Springiness = 4, EasingMode = EasingMode.EaseOut },
                "Spin" => new CubicEase { EasingMode = EasingMode.EaseInOut },
                _ => new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var spinDegrees = animType switch
            {
                "Spin" => 720,
                "Bounce" => 0,
                _ => 180
            };

            var flyX = Animate(start.X - halfWidth, target.X - halfWidth, flyMs, flyEase);
            var flyY = Animate(start.Y - halfHeight, target.Y - halfHeight, flyMs, flyEase);
            var growX = Animate(1.0, 1.35, flyMs, new CubicEase { EasingMode = EasingMode.EaseIn });
            var growY = Animate(1.0, 1.35, flyMs, new CubicEase { EasingMode = EasingMode.EaseIn });
            var spin = spinDegrees > 0
                ? Animate(0, spinDegrees, flyMs, new CubicEase { EasingMode = EasingMode.EaseIn })
                : null;

            flyX.Completed += (_, _) =>
            {
                TriggerImpact();

                var impactX = Animate(scale.ScaleX, 0.9, pauseMs * 0.4,
                    new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 1 });
                var impactY = Animate(scale.ScaleY, 0.9, pauseMs * 0.4,
                    new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 1 });

                impactX.Completed += (_, _) =>
                    ReturnAndRemove(
                        flyer,
                        scale,
                        rotate,
                        start,
                        halfWidth,
                        halfHeight,
                        returnMs,
                        pauseMs,
                        fadeMs,
                        spinDegrees,
                        TriggerComplete);

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, impactX);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, impactY);
            };

            flyer.BeginAnimation(Canvas.LeftProperty, flyX);
            flyer.BeginAnimation(Canvas.TopProperty, flyY);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, growX);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, growY);

            if (spin != null)
                rotate.BeginAnimation(RotateTransform.AngleProperty, spin);
        }

        private void ReturnAndRemove(
            FrameworkElement flyer,
            ScaleTransform scale,
            RotateTransform rotate,
            Point start,
            double halfWidth,
            double halfHeight,
            double returnMs,
            double pauseMs,
            double fadeMs,
            double spinDegrees,
            Action? onComplete)
        {
            var begin = TimeSpan.FromMilliseconds(pauseMs * 0.6);
            var retX = new DoubleAnimation(Canvas.GetLeft(flyer), start.X - halfWidth, TimeSpan.FromMilliseconds(returnMs))
            {
                BeginTime = begin,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var retY = new DoubleAnimation(Canvas.GetTop(flyer), start.Y - halfHeight, TimeSpan.FromMilliseconds(returnMs))
            {
                BeginTime = begin,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var shrinkX = new DoubleAnimation(scale.ScaleX, 1.0, TimeSpan.FromMilliseconds(returnMs))
            {
                BeginTime = begin
            };
            var shrinkY = new DoubleAnimation(scale.ScaleY, 1.0, TimeSpan.FromMilliseconds(returnMs))
            {
                BeginTime = begin
            };
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(fadeMs))
            {
                BeginTime = TimeSpan.FromMilliseconds(pauseMs * 0.6 + Math.Max(0, returnMs - fadeMs))
            };

            fade.Completed += (_, _) =>
            {
                RemoveFlyer(flyer);
                _busy = false;
                onComplete?.Invoke();
            };

            flyer.BeginAnimation(Canvas.LeftProperty, retX);
            flyer.BeginAnimation(Canvas.TopProperty, retY);
            flyer.BeginAnimation(UIElement.OpacityProperty, fade);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrinkX);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrinkY);

            if (spinDegrees > 0)
            {
                var spinBack = new DoubleAnimation(rotate.Angle, 0, TimeSpan.FromMilliseconds(returnMs))
                {
                    BeginTime = begin,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                rotate.BeginAnimation(RotateTransform.AngleProperty, spinBack);
            }
        }

        private static FrameworkElement? CreateFlyer(string mediaPath, double mediaSpeed)
        {
            var extension = Path.GetExtension(mediaPath).ToLowerInvariant();
            var speed = Math.Max(0.1, mediaSpeed);

            return extension switch
            {
                ".webm" or ".mp4" => CreateBrowserVideoFlyer(),
                ".avi" => CreateNativeVideoFlyer(mediaPath, speed),
                _ => CreateGifFlyer(mediaPath, speed)
            };
        }

        private static Image? CreateGifFlyer(string path, double speed)
        {
            try
            {
                var source = new BitmapImage();
                source.BeginInit();
                source.UriSource = new Uri(path, UriKind.Absolute);
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.EndInit();
                source.Freeze();

                var image = CreateBaseFlyer<Image>();
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
                ImageBehavior.SetAnimatedSource(image, source);
                ImageBehavior.SetRepeatBehavior(image, RepeatBehavior.Forever);
                ImageBehavior.SetAnimationSpeedRatio(image, speed);
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static WebView2 CreateBrowserVideoFlyer()
        {
            var video = CreateBaseFlyer<WebView2>();
            video.IsHitTestVisible = false;
            return video;
        }

        private static MediaElement CreateNativeVideoFlyer(string path, double speed)
        {
            var video = CreateBaseFlyer<MediaElement>();
            video.Source = new Uri(path, UriKind.Absolute);
            video.LoadedBehavior = MediaState.Manual;
            video.UnloadedBehavior = MediaState.Stop;
            video.Stretch = Stretch.Uniform;
            video.SpeedRatio = speed;
            video.Volume = 0;
            video.MediaEnded += (_, _) =>
            {
                video.Position = TimeSpan.Zero;
                video.Play();
            };
            return video;
        }

        private static T CreateBaseFlyer<T>() where T : FrameworkElement, new()
        {
            return new T
            {
                Width = 130,
                Height = 130,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new TransformGroup
                {
                    Children =
                    {
                        new ScaleTransform(1, 1),
                        new RotateTransform(0)
                    }
                }
            };
        }

        private static void StartNativeVideo(MediaElement video)
        {
            video.Dispatcher.BeginInvoke(new Action(() =>
            {
                video.Position = TimeSpan.Zero;
                video.Play();
            }));
        }

        private static void StartBrowserVideo(WebView2 video, string path, double speed, Action onFailed)
        {
            _ = StartBrowserVideoAsync(video, path, speed, onFailed);
        }

        private static async Task StartBrowserVideoAsync(WebView2 video, string path, double speed, Action onFailed)
        {
            try
            {
                await video.EnsureCoreWebView2Async();
                video.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                video.CoreWebView2.Settings.AreDevToolsEnabled = false;
                BrowserVideoDocument.MapMediaFolder(video, path);
                video.NavigateToString(BrowserVideoDocument.Build(path, speed));
            }
            catch
            {
                onFailed();
            }
        }

        private void RemoveFlyer(FrameworkElement flyer)
        {
            flyer.BeginAnimation(Canvas.LeftProperty, null);
            flyer.BeginAnimation(Canvas.TopProperty, null);
            flyer.BeginAnimation(UIElement.OpacityProperty, null);

            if (flyer.RenderTransform is TransformGroup transform)
            {
                foreach (var child in transform.Children)
                {
                    if (child is ScaleTransform scale)
                    {
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    }
                    else if (child is RotateTransform rotate)
                    {
                        rotate.BeginAnimation(RotateTransform.AngleProperty, null);
                    }
                }
            }

            if (flyer is Image image)
                ImageBehavior.SetAnimatedSource(image, null);

            if (flyer is MediaElement nativeVideo)
            {
                nativeVideo.Stop();
                nativeVideo.Source = null;
            }

            _overlay.Children.Remove(flyer);

            if (flyer is WebView2 browserVideo)
                browserVideo.Dispose();
        }

        private static DoubleAnimation Animate(double from, double to, double ms, IEasingFunction ease) => new()
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(ms),
            EasingFunction = ease
        };
    }
}
