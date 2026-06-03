using System;
using System.Globalization;
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
    /// <summary>
    /// Plays the side-panel idle/search animation. Kill/block media is handled by KillAnimator.
    /// </summary>
    public class GifPlayer
    {
        private readonly Grid _host;
        private Image? _gifImg;
        private MediaElement? _vidEl;
        private WebView2? _webView;
        private string? _curPath;
        private double _curSpeed = -1;

        public GifPlayer(Grid host) => _host = host;

        public void Play(string path, double speed = 1.0, bool force = false)
        {
            if (!force && path == _curPath && Math.Abs(speed - _curSpeed) < 0.001)
                return;

            StopAll();
            _curPath = path;
            _curSpeed = speed;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".webm" or ".mp4")
                PlayBrowserVideo(path, speed);
            else if (ext == ".avi")
                PlayNativeVideo(path, speed);
            else
                PlayGif(path, speed);
        }

        public void SetSpeed(double speed)
        {
            _curSpeed = speed;
            if (_gifImg != null)
                ImageBehavior.SetAnimationSpeedRatio(_gifImg, SafeSpeed(speed));
            if (_vidEl != null)
                _vidEl.SpeedRatio = SafeSpeed(speed);
            if (_webView?.CoreWebView2 != null)
            {
                var rate = SafeSpeed(speed).ToString(CultureInfo.InvariantCulture);
                _ = _webView.ExecuteScriptAsync($"(function(){{const video=document.getElementById('v');if(video)video.playbackRate={rate};}})();");
            }
        }

        public void Stop()
        {
            StopAll();
            _curPath = null;
            _curSpeed = -1;
        }

        private void PlayGif(string path, double speed)
        {
            try
            {
                _gifImg = new Image
                {
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(_gifImg, BitmapScalingMode.HighQuality);

                var src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(path, UriKind.Absolute);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
                src.Freeze();

                ImageBehavior.SetAnimatedSource(_gifImg, src);
                ImageBehavior.SetRepeatBehavior(_gifImg, RepeatBehavior.Forever);
                ImageBehavior.SetAnimationSpeedRatio(_gifImg, SafeSpeed(speed));

                _host.Children.Add(_gifImg);
            }
            catch
            {
            }
        }

        private void PlayBrowserVideo(string path, double speed)
        {
            try
            {
                _webView = new WebView2
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    IsHitTestVisible = false
                };

                _host.Children.Add(_webView);
                _ = LoadBrowserVideoAsync(_webView, path, speed);
            }
            catch
            {
                Stop();
            }
        }

        private async Task LoadBrowserVideoAsync(WebView2 webView, string path, double speed)
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                if (_webView != webView)
                    return;

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                BrowserVideoDocument.MapMediaFolder(webView, path);
                webView.NavigateToString(BrowserVideoDocument.Build(path, speed));
            }
            catch
            {
                if (_webView == webView)
                    Stop();
            }
        }

        private void PlayNativeVideo(string path, double speed)
        {
            try
            {
                _vidEl = new MediaElement
                {
                    Source = new Uri(path, UriKind.Absolute),
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Stop,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    SpeedRatio = SafeSpeed(speed),
                    Volume = 0
                };
                _vidEl.MediaOpened += (_, _) => _vidEl.Play();
                _vidEl.MediaEnded += (_, _) =>
                {
                    _vidEl.Position = TimeSpan.Zero;
                    _vidEl.Play();
                };
                _vidEl.MediaFailed += (_, _) => Stop();
                _host.Children.Add(_vidEl);
                _vidEl.Play();
            }
            catch
            {
            }
        }

        private void StopAll()
        {
            if (_gifImg != null)
            {
                ImageBehavior.SetAnimatedSource(_gifImg, null);
                _host.Children.Remove(_gifImg);
                _gifImg = null;
            }

            if (_vidEl != null)
            {
                _vidEl.Stop();
                _vidEl.Source = null;
                _host.Children.Remove(_vidEl);
                _vidEl = null;
            }

            if (_webView != null)
            {
                _host.Children.Remove(_webView);
                _webView.Dispose();
                _webView = null;
            }
        }

        private static double SafeSpeed(double speed) => Math.Max(0.1, speed);
    }
}
