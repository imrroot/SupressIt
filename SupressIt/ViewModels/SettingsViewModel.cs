using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SupressIt.Helpers;
using SupressIt.Models;

namespace SupressIt.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly AppSettings _s;

        public SettingsViewModel(AppSettings s)
        {
            _s = s;
            GifFileResolver.EnsureDefaultFolder();
            SoundFileResolver.EnsureDefaultFolder();
        }

        // ── Mode ─────────────────────────────────────────────────────────────
        public bool IsDarkMode
        {
            get => _s.IsDarkMode;
            set
            {
                _s.IsDarkMode = value; P();
                if (value) ThemeManager.ApplyDarkPreset(_s);
                else       ThemeManager.ApplyLightPreset(_s);
                NotifyAllColors();
                Fire();
            }
        }

        // ── Per-zone colours ──────────────────────────────────────────────────
        public string ColorShellBg      { get => _s.ColorShellBg;      set { _s.ColorShellBg      = value; P(); Fire(); } }
        public string ColorCardBg       { get => _s.ColorCardBg;       set { _s.ColorCardBg       = value; P(); Fire(); } }
        public string ColorTabBarBg     { get => _s.ColorTabBarBg;     set { _s.ColorTabBarBg     = value; P(); Fire(); } }
        public string ColorTextPrimary  { get => _s.ColorTextPrimary;  set { _s.ColorTextPrimary  = value; P(); Fire(); } }
        public string ColorTextSecond   { get => _s.ColorTextSecond;   set { _s.ColorTextSecond   = value; P(); Fire(); } }
        public string ColorTextSoft     { get => _s.ColorTextSoft;     set { _s.ColorTextSoft     = value; P(); Fire(); } }
        public string ColorAccent       { get => _s.ColorAccent;       set { _s.ColorAccent       = value; P(); Fire(); } }
        public string ColorAccent2      { get => _s.ColorAccent2;      set { _s.ColorAccent2      = value; P(); Fire(); } }
        public string ColorKillBtn      { get => _s.ColorKillBtn;      set { _s.ColorKillBtn      = value; P(); Fire(); } }
        public string ColorBlockBtn     { get => _s.ColorBlockBtn;     set { _s.ColorBlockBtn     = value; P(); Fire(); } }
        public string ColorLogBg        { get => _s.ColorLogBg;        set { _s.ColorLogBg        = value; P(); Fire(); } }
        public string ColorLogText      { get => _s.ColorLogText;      set { _s.ColorLogText      = value; P(); Fire(); } }
        public string ColorLogTag       { get => _s.ColorLogTag;       set { _s.ColorLogTag       = value; P(); Fire(); } }
        public string ColorAnimePanelBg { get => _s.ColorAnimePanelBg; set { _s.ColorAnimePanelBg = value; P(); Fire(); } }
        public string ColorTabChecked   { get => _s.ColorTabChecked;   set { _s.ColorTabChecked   = value; P(); Fire(); } }
        public string ColorTabHover     { get => _s.ColorTabHover;     set { _s.ColorTabHover     = value; P(); Fire(); } }

        // ── Background ───────────────────────────────────────────────────────
        public bool   UseCustomBackground { get => _s.UseCustomBackground; set { _s.UseCustomBackground = value; P(); Save(); BackgroundChanged?.Invoke(); } }
        public string BackgroundPath      { get => _s.BackgroundPath;      set { _s.BackgroundPath      = value; P(); Save(); BackgroundChanged?.Invoke(); } }
        public double BackgroundOpacity   { get => _s.BackgroundOpacity;   set { _s.BackgroundOpacity   = value; P(); Save(); BackgroundChanged?.Invoke(); } }
        public double ElementsOpacity     { get => _s.ElementsOpacity;     set { _s.ElementsOpacity     = value; P(); Save(); ElementsOpacityChanged?.Invoke(value); } }

        // ── GIF ───────────────────────────────────────────────────────────────
        public bool   GifsEnabled         { get => _s.GifsEnabled;         set { _s.GifsEnabled         = value; P(); Save(); GifSettingsChanged?.Invoke("all"); } }
        public bool   UseDefaultGifFolder { get => _s.UseDefaultGifFolder; set { _s.UseDefaultGifFolder = value; P(); NotifyGifPaths(); Save(); GifSettingsChanged?.Invoke("all"); } }
        public string DefaultGifFolderPath => GifFileResolver.DefaultFolderPath;
        public string EffectiveGifNormalPath    => GifFileResolver.Resolve(_s.UseDefaultGifFolder, GifSlot.Idle,   _s.GifNormalPath);
        public string EffectiveGifSearchingPath => GifFileResolver.Resolve(_s.UseDefaultGifFolder, GifSlot.Search, _s.GifSearchingPath);
        public string EffectiveGifKillPath      => GifFileResolver.Resolve(_s.UseDefaultGifFolder, GifSlot.Kill,   _s.GifKillPath);
        public string EffectiveGifBlockPath     => GifFileResolver.Resolve(_s.UseDefaultGifFolder, GifSlot.Block,  _s.GifBlockPath);
        public string DisplayGifNormalPath      => GifFileResolver.GetDisplayPath(_s.UseDefaultGifFolder, GifSlot.Idle,   _s.GifNormalPath);
        public string DisplayGifSearchingPath   => GifFileResolver.GetDisplayPath(_s.UseDefaultGifFolder, GifSlot.Search, _s.GifSearchingPath);
        public string DisplayGifKillPath        => GifFileResolver.GetDisplayPath(_s.UseDefaultGifFolder, GifSlot.Kill,   _s.GifKillPath);
        public string DisplayGifBlockPath       => GifFileResolver.GetDisplayPath(_s.UseDefaultGifFolder, GifSlot.Block,  _s.GifBlockPath);
        public string GifNormalPath       { get => _s.GifNormalPath;       set { _s.GifNormalPath       = value; P(); NotifyGifPaths(); Save(); GifSettingsChanged?.Invoke("normal"); } }
        public string GifSearchingPath    { get => _s.GifSearchingPath;    set { _s.GifSearchingPath    = value; P(); NotifyGifPaths(); Save(); GifSettingsChanged?.Invoke("searching"); } }
        public string GifKillPath         { get => _s.GifKillPath;         set { _s.GifKillPath         = value; P(); NotifyGifPaths(); Save(); GifSettingsChanged?.Invoke("kill"); } }
        public string GifBlockPath        { get => _s.GifBlockPath;        set { _s.GifBlockPath        = value; P(); NotifyGifPaths(); Save(); GifSettingsChanged?.Invoke("block"); } }
        public double GifNormalSpeed      { get => _s.GifNormalSpeed;      set { _s.GifNormalSpeed      = value; P(); Save(); GifSettingsChanged?.Invoke("normalSpeed"); } }
        public double GifSearchingSpeed   { get => _s.GifSearchingSpeed;   set { _s.GifSearchingSpeed   = value; P(); Save(); GifSettingsChanged?.Invoke("searchingSpeed"); } }
        public double GifKillSpeed        { get => _s.GifKillSpeed;        set { _s.GifKillSpeed        = value; P(); Save(); GifSettingsChanged?.Invoke("killSpeed"); } }
        public double GifBlockSpeed       { get => _s.GifBlockSpeed;       set { _s.GifBlockSpeed       = value; P(); Save(); GifSettingsChanged?.Invoke("blockSpeed"); } }
        public string KillAnimationType   { get => _s.KillAnimationType;   set { _s.KillAnimationType   = value; P(); Save(); GifSettingsChanged?.Invoke("killType"); } }
        public double KillAnimationSpeed  { get => _s.KillAnimationSpeed;  set { _s.KillAnimationSpeed  = value; P(); Save(); GifSettingsChanged?.Invoke("killSpeed"); } }
        public string ItemDeathAnimation  { get => _s.ItemDeathAnimation;  set { _s.ItemDeathAnimation  = value; P(); Save(); GifSettingsChanged?.Invoke("deathType"); } }
        public double ItemDeathDuration   { get => _s.ItemDeathDuration;   set { _s.ItemDeathDuration   = value; P(); Save(); GifSettingsChanged?.Invoke("deathDuration"); } }

        // ── Sound ─────────────────────────────────────────────────────────────
        public bool   SoundEnabled        { get => _s.SoundEnabled;        set { _s.SoundEnabled        = value; P(); Save(); } }
        public bool   UseDefaultSoundFolder { get => _s.UseDefaultSoundFolder; set { _s.UseDefaultSoundFolder = value; P(); NotifySoundPaths(); Save(); } }
        public string DefaultSoundFolderPath => SoundFileResolver.DefaultFolderPath;
        public string EffectiveSoundNormalPath    => SoundFileResolver.Resolve(_s.UseDefaultSoundFolder, SoundSlot.Idle,   _s.SoundNormalPath);
        public string EffectiveSoundSearchingPath => SoundFileResolver.Resolve(_s.UseDefaultSoundFolder, SoundSlot.Search, _s.SoundSearchingPath);
        public string EffectiveSoundKillPath      => SoundFileResolver.Resolve(_s.UseDefaultSoundFolder, SoundSlot.Kill,   _s.SoundKillPath);
        public string EffectiveSoundBlockPath     => SoundFileResolver.Resolve(_s.UseDefaultSoundFolder, SoundSlot.Block,  _s.SoundBlockPath);
        public string DisplaySoundNormalPath      => SoundFileResolver.GetDisplayPath(_s.UseDefaultSoundFolder, SoundSlot.Idle,   _s.SoundNormalPath);
        public string DisplaySoundSearchingPath   => SoundFileResolver.GetDisplayPath(_s.UseDefaultSoundFolder, SoundSlot.Search, _s.SoundSearchingPath);
        public string DisplaySoundKillPath        => SoundFileResolver.GetDisplayPath(_s.UseDefaultSoundFolder, SoundSlot.Kill,   _s.SoundKillPath);
        public string DisplaySoundBlockPath       => SoundFileResolver.GetDisplayPath(_s.UseDefaultSoundFolder, SoundSlot.Block,  _s.SoundBlockPath);
        public string SoundNormalPath     { get => _s.SoundNormalPath;     set { _s.SoundNormalPath     = value; P(); NotifySoundPaths(); Save(); } }
        public string SoundSearchingPath  { get => _s.SoundSearchingPath;  set { _s.SoundSearchingPath  = value; P(); NotifySoundPaths(); Save(); } }
        public string SoundKillPath       { get => _s.SoundKillPath;       set { _s.SoundKillPath       = value; P(); NotifySoundPaths(); Save(); } }
        public string SoundBlockPath      { get => _s.SoundBlockPath;      set { _s.SoundBlockPath      = value; P(); NotifySoundPaths(); Save(); } }
        public double SoundVolume         { get => _s.SoundVolume;         set { _s.SoundVolume         = value; P(); Save(); } }
        public double SoundSpeed          { get => _s.SoundSpeed;          set { _s.SoundSpeed          = value; P(); Save(); } }

        // ── General ───────────────────────────────────────────────────────────
        public bool   IconAnimationsEnabled { get => _s.IconAnimationsEnabled; set { _s.IconAnimationsEnabled = value; P(); Save(); } }
        public bool   GlobalNetLimitEnabled { get => _s.GlobalNetLimitEnabled; set { _s.GlobalNetLimitEnabled = value; P(); Save(); } }
        public double GlobalNetLimitMb      { get => _s.GlobalNetLimitMb;      set { _s.GlobalNetLimitMb      = value; P(); Save(); } }
        public bool   RunAtStartup          { get => _s.RunAtStartup;          set { _s.RunAtStartup          = value; P(); Save(); StartupManager.SetStartup(value); } }

        // ── Preset helpers ────────────────────────────────────────────────────
        public void ApplyAccentPreset(string name)
        {
            ThemeManager.ApplyAccentPreset(_s, name);
            NotifyAllColors();
            Fire();
        }

        private void NotifyAllColors()
        {
            P(nameof(ColorShellBg));      P(nameof(ColorCardBg));
            P(nameof(ColorTabBarBg));     P(nameof(ColorTextPrimary));
            P(nameof(ColorTextSecond));   P(nameof(ColorTextSoft));
            P(nameof(ColorAccent));       P(nameof(ColorAccent2));
            P(nameof(ColorKillBtn));      P(nameof(ColorBlockBtn));
            P(nameof(ColorLogBg));        P(nameof(ColorLogText));
            P(nameof(ColorLogTag));       P(nameof(ColorAnimePanelBg));
            P(nameof(ColorTabChecked));   P(nameof(ColorTabHover));
        }

        // ── Events ────────────────────────────────────────────────────────────
        private void NotifyGifPaths()
        {
            P(nameof(DefaultGifFolderPath));
            P(nameof(EffectiveGifNormalPath));
            P(nameof(EffectiveGifSearchingPath));
            P(nameof(EffectiveGifKillPath));
            P(nameof(EffectiveGifBlockPath));
            P(nameof(DisplayGifNormalPath));
            P(nameof(DisplayGifSearchingPath));
            P(nameof(DisplayGifKillPath));
            P(nameof(DisplayGifBlockPath));
        }

        private void NotifySoundPaths()
        {
            P(nameof(DefaultSoundFolderPath));
            P(nameof(EffectiveSoundNormalPath));
            P(nameof(EffectiveSoundSearchingPath));
            P(nameof(EffectiveSoundKillPath));
            P(nameof(EffectiveSoundBlockPath));
            P(nameof(DisplaySoundNormalPath));
            P(nameof(DisplaySoundSearchingPath));
            P(nameof(DisplaySoundKillPath));
            P(nameof(DisplaySoundBlockPath));
        }

        public event Action         ThemeChanged;
        public event Action         BackgroundChanged;
        /// <summary>Fired on any GIF setting change. Arg: "normal","searching","kill","block","all","normalSpeed","searchingSpeed","killSpeed","blockSpeed"</summary>
        public event Action<string> GifSettingsChanged;
        public event Action<double> ElementsOpacityChanged;

        // ── Helpers ───────────────────────────────────────────────────────────
        public AppSettings Raw => _s;

        private void Fire() { ThemeManager.Apply(_s); ThemeChanged?.Invoke(); Save(); }
        public void Save()  => SettingsStore.Save(_s);

        public event PropertyChangedEventHandler PropertyChanged;
        private void P([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
