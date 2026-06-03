using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SupressIt.Helpers;
using SupressIt.ViewModels;
using SupressIt.Views.Panels;
using AnimatedImage.Wpf;

namespace SupressIt
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly SettingsViewModel _settingsVm;
        private readonly ISoundPlayer _soundPlayer;
        private readonly GifPlayer _panelGifPlayer;
        private readonly KillAnimator _killAnimator;
        private AnimeState _panelState = AnimeState.Normal;

        public MainWindow()
        {
            var settings = SettingsStore.Load();
            ThemeManager.Apply(settings);

            InitializeComponent();

            _settingsVm = new SettingsViewModel(settings);
            _settingsVm.BackgroundChanged += ApplyBackground;
            _settingsVm.ThemeChanged += ApplyShellBackground;
            _settingsVm.ElementsOpacityChanged += ApplyElementsOpacity;
            _settingsVm.GifSettingsChanged += OnGifSettingsChanged;

            _vm = new MainViewModel();
            _vm.KillLogUpdated += () => LogScroll.ScrollToTop();
            _vm.AnimeStateChanged += OnAnimeStateChanged;

            _soundPlayer = new SoundManager(settings);
            _panelGifPlayer = new GifPlayer(GifContainer);
            _killAnimator = new KillAnimator(OverlayCanvas);

            DataContext = _vm;
            PanelSettings.DataContext = _settingsVm;

            PanelApps.KillRequested += OnProcessActionRequested;
            PanelSystem.ToggleRequested += name => _vm.ToggleService(name);
            PanelSystem.BlockRequested += name => _vm.StopAndBlacklistService(name);
            PanelInternet.BlockRequested += OnNetworkBlockRequested;
            PanelStartup.ToggleRequested += entry => _vm.ToggleStartup(entry);
            PanelBlocked.ToggleActiveRequested += (id, active) => _vm.SetBlacklistActive(id, active);
            PanelBlocked.RemoveRequested += id => _vm.RemoveFromBlacklist(id);

            Loaded += (_, _) =>
            {
                ApplyShellBackground();
                ApplyBackground();
                ApplyElementsOpacity(_settingsVm.ElementsOpacity);
                SetupAdminBadge();

                _panelState = AnimeState.Normal;
                PlayPanelGif(AnimeState.Normal);
                SetLabel(AnimeState.Normal);
                _soundPlayer.Play(SoundHint.Normal);
            };
        }

        private void SetupAdminBadge()
        {
            AdminBadge.Visibility = StartupManager.IsAdmin
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void RunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "SupressIt needs Administrator rights to kill system processes and stop services.\n\n" +
                "Restart as Administrator now?",
                "Run as Administrator",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                StartupManager.RestartAsAdmin();
        }

        private void ApplyShellBackground()
        {
            if (TryFindResource("BgShell") is SolidColorBrush brush)
                ShellBorder.Background = brush;
        }

        private void ApplyElementsOpacity(double opacity)
        {
            foreach (var element in new FrameworkElement[]
            {
                PanelApps,
                PanelSystem,
                PanelInternet,
                PanelStartup,
                PanelBlocked,
                PanelSettings,
                AnimeSidePanel
            })
            {
                element.Opacity = opacity;
            }
        }

        private void ApplyBackground()
        {
            BgImage.BeginAnimation(Image.SourceProperty, null);
            ImageBehavior.SetAnimatedSource(BgImage, null);
            BgImage.Source = null;
            BgImage.Visibility = Visibility.Collapsed;

            BgVideo.MediaEnded -= BgVideo_MediaEnded;
            BgVideo.Stop();
            BgVideo.Source = null;
            BgVideo.Visibility = Visibility.Collapsed;

            if (!_settingsVm.UseCustomBackground)
                return;

            var path = _settingsVm.BackgroundPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            var opacity = _settingsVm.BackgroundOpacity;
            var extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension is ".webm" or ".mp4")
            {
                BgVideo.Source = new Uri(path, UriKind.Absolute);
                BgVideo.Opacity = opacity;
                BgVideo.Visibility = Visibility.Visible;
                BgVideo.MediaEnded += BgVideo_MediaEnded;
                BgVideo.Play();
                return;
            }

            if (extension == ".gif")
            {
                try
                {
                    var source = LoadBitmap(path);
                    ImageBehavior.SetAnimatedSource(BgImage, source);
                    ImageBehavior.SetRepeatBehavior(BgImage, RepeatBehavior.Forever);
                    BgImage.Opacity = opacity;
                    BgImage.Visibility = Visibility.Visible;
                }
                catch
                {
                }

                return;
            }

            try
            {
                BgImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                BgImage.Opacity = opacity;
                BgImage.Visibility = Visibility.Visible;
            }
            catch
            {
            }
        }

        private void BgVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            BgVideo.Position = TimeSpan.Zero;
            BgVideo.Play();
        }

        private void PlayPanelGif(AnimeState logicalState)
        {
            if (!_settingsVm.GifsEnabled)
            {
                _panelGifPlayer.Stop();
                return;
            }

            var isSearching = logicalState == AnimeState.Searching;
            _panelState = isSearching ? AnimeState.Searching : AnimeState.Normal;

            var path = isSearching
                ? _settingsVm.EffectiveGifSearchingPath
                : _settingsVm.EffectiveGifNormalPath;
            var speed = isSearching
                ? _settingsVm.GifSearchingSpeed
                : _settingsVm.GifNormalSpeed;

            _panelGifPlayer.Play(path, speed);
        }

        private void OnGifSettingsChanged(string setting)
        {
            if (!_settingsVm.GifsEnabled)
            {
                _panelGifPlayer.Stop();
                return;
            }

            if (setting == "normalSpeed" && _panelState == AnimeState.Normal)
            {
                _panelGifPlayer.SetSpeed(_settingsVm.GifNormalSpeed);
                return;
            }

            if (setting == "searchingSpeed" && _panelState == AnimeState.Searching)
            {
                _panelGifPlayer.SetSpeed(_settingsVm.GifSearchingSpeed);
                return;
            }

            if (setting is "kill" or "block" or "killSpeed" or "blockSpeed" or "killType" or "deathType" or "deathDuration")
                return;

            _panelGifPlayer.Play(
                _panelState == AnimeState.Searching
                    ? _settingsVm.EffectiveGifSearchingPath
                    : _settingsVm.EffectiveGifNormalPath,
                _panelState == AnimeState.Searching
                    ? _settingsVm.GifSearchingSpeed
                    : _settingsVm.GifNormalSpeed);
        }

        private void OnAnimeStateChanged(AnimeState state)
        {
            SetLabel(state);
            PlayStateSound(state);

            var wantsSearching = state == AnimeState.Searching;
            var isSearching = _panelState == AnimeState.Searching;

            if (wantsSearching != isSearching)
                PlayPanelGif(state);
        }

        private void PlayStateSound(AnimeState state)
        {
            var sound = state switch
            {
                AnimeState.Searching => SoundHint.Searching,
                AnimeState.Killing => SoundHint.Kill,
                AnimeState.Blocking => SoundHint.Block,
                _ => SoundHint.Normal
            };

            _soundPlayer.Play(sound);
        }

        private void SetLabel(AnimeState state)
        {
            switch (state)
            {
                case AnimeState.Searching:
                    AnimeLabel.Text = "( searching... )";
                    AnimeTip.Text = "Looking through the list!";
                    break;
                case AnimeState.Killing:
                    AnimeLabel.Text = "( SMASH!!! )";
                    AnimeTip.Text = "Fly my minion!";
                    break;
                case AnimeState.Blocking:
                    AnimeLabel.Text = "( BLOCKED! )";
                    AnimeTip.Text = "You shall not pass!";
                    break;
                default:
                    AnimeLabel.Text = "( chilling~ )";
                    AnimeTip.Text = "Click the hammer to smash an app!";
                    break;
            }
        }

        private void OnProcessActionRequested(object sender, ProcessActionRequestedEventArgs request)
        {
            var version = _vm.BeginDestructiveAction(request.AddToBlacklist);

            PlayActionAnimation(
                request.AddToBlacklist,
                request.TargetScreenPoint,
                () => PlayTargetDeath(
                    request.TargetElement,
                    () => _vm.KillProcess(request.ProcessId, request.AddToBlacklist, version)));
        }

        private void OnNetworkBlockRequested(object sender, NetworkBlockRequestedEventArgs request)
        {
            var version = _vm.BeginDestructiveAction(true);

            PlayActionAnimation(
                true,
                request.TargetScreenPoint,
                () => PlayTargetDeath(
                    request.TargetElement,
                    () => _vm.BlockNetworkProcess(request.ProcessName, version)));
        }

        private void PlayActionAnimation(bool block, Point targetScreen, Action onImpact)
        {
            var gifPath = block ? _settingsVm.EffectiveGifBlockPath : _settingsVm.EffectiveGifKillPath;
            var gifSpeed = block ? _settingsVm.GifBlockSpeed : _settingsVm.GifKillSpeed;

            if (!_settingsVm.GifsEnabled || string.IsNullOrWhiteSpace(gifPath) || !File.Exists(gifPath))
            {
                onImpact();
                return;
            }

            var startScreen = GifContainer.PointToScreen(
                new Point(GifContainer.ActualWidth / 2, GifContainer.ActualHeight / 2));

            _killAnimator.Fly(
                gifPath,
                startScreen,
                targetScreen,
                gifSpeed,
                _settingsVm.KillAnimationSpeed,
                _settingsVm.KillAnimationType,
                onImpact);
        }

        private void PlayTargetDeath(FrameworkElement? target, Action onComplete)
        {
            if (target != null && _settingsVm.ItemDeathAnimation != "None")
            {
                DeathAnimator.Play(
                    target,
                    _settingsVm.ItemDeathAnimation,
                    _settingsVm.ItemDeathDuration,
                    onComplete);
                return;
            }

            onComplete();
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button)
                return;

            foreach (var tab in new[] { TabApps, TabSystem, TabInternet, TabStartup, TabBlocked, TabSettings })
                tab.IsChecked = false;

            button.IsChecked = true;

            PanelApps.Visibility = Visibility.Collapsed;
            PanelSystem.Visibility = Visibility.Collapsed;
            PanelInternet.Visibility = Visibility.Collapsed;
            PanelStartup.Visibility = Visibility.Collapsed;
            PanelBlocked.Visibility = Visibility.Collapsed;
            PanelSettings.Visibility = Visibility.Collapsed;

            switch (button.Tag?.ToString())
            {
                case "Apps":
                    PanelApps.Visibility = Visibility.Visible;
                    _vm.ActiveTab = ActiveTab.Processes;
                    break;
                case "System":
                    PanelSystem.Visibility = Visibility.Visible;
                    _vm.ActiveTab = ActiveTab.Services;
                    break;
                case "Internet":
                    PanelInternet.Visibility = Visibility.Visible;
                    _vm.ActiveTab = ActiveTab.Network;
                    break;
                case "Startup":
                    PanelStartup.Visibility = Visibility.Visible;
                    _vm.ActiveTab = ActiveTab.Startup;
                    break;
                case "Blocked":
                    PanelBlocked.Visibility = Visibility.Visible;
                    _vm.ActiveTab = ActiveTab.Blacklist;
                    break;
                case "Settings":
                    PanelSettings.Visibility = Visibility.Visible;
                    _vm.ActiveTab = ActiveTab.Settings;
                    break;
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e) => _vm.ClearLog();

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _vm.Shutdown();
            _settingsVm.Save();
            Close();
        }

        private static BitmapImage LoadBitmap(string path)
        {
            var source = new BitmapImage();
            source.BeginInit();
            source.UriSource = new Uri(path, UriKind.Absolute);
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.EndInit();
            source.Freeze();
            return source;
        }
    }
}
