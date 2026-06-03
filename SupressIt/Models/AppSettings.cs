namespace SupressIt.Models
{
    public class AppSettings
    {
        // ── Mode ──────────────────────────────────────────────────────────────
        public bool   IsDarkMode            { get; set; } = true;

        // ── Granular colours (every zone) ─────────────────────────────────────
        public string ColorShellBg          { get; set; } = "#140E22";
        public string ColorCardBg           { get; set; } = "#221838";
        public string ColorTabBarBg         { get; set; } = "#181028";
        public string ColorTextPrimary      { get; set; } = "#E8D8FF";
        public string ColorTextSecond       { get; set; } = "#A088C8";
        public string ColorTextSoft         { get; set; } = "#605080";
        public string ColorAccent           { get; set; } = "#8050C0";
        public string ColorAccent2          { get; set; } = "#C050A0";
        public string ColorKillBtn          { get; set; } = "#FF4060";
        public string ColorBlockBtn         { get; set; } = "#8050C0";
        public string ColorLogBg            { get; set; } = "#180A2A";
        public string ColorLogText          { get; set; } = "#FF90D0";
        public string ColorLogTag           { get; set; } = "#605080";
        public string ColorAnimePanelBg     { get; set; } = "#201436";
        public string ColorTabChecked       { get; set; } = "#308050C0";
        public string ColorTabHover         { get; set; } = "#188050C0";

        // ── Background ────────────────────────────────────────────────────────
        public bool   UseCustomBackground   { get; set; } = false;
        public string BackgroundPath        { get; set; } = "";
        public double BackgroundOpacity     { get; set; } = 0.50;
        // Opacity of UI elements (cards, panels) so background shows through
        public double ElementsOpacity       { get; set; } = 0.92;

        // ── GIF ───────────────────────────────────────────────────────────────
        public bool   GifsEnabled           { get; set; } = true;
        public bool   UseDefaultGifFolder   { get; set; } = false;
        public string GifNormalPath         { get; set; } = "";
        public string GifSearchingPath      { get; set; } = "";
        public string GifKillPath           { get; set; } = "";
        public string GifBlockPath          { get; set; } = "";
        public double GifNormalSpeed        { get; set; } = 1.0;
        public double GifSearchingSpeed     { get; set; } = 1.0;
        public double GifKillSpeed          { get; set; } = 1.0;
        public double GifBlockSpeed         { get; set; } = 1.0;

        // Kill fly animation
        public string KillAnimationType     { get; set; } = "Fly";   // Fly | Bounce | Spin
        public double KillAnimationSpeed    { get; set; } = 1.0;

        // Item death animation when killed
        public string ItemDeathAnimation    { get; set; } = "Burn";  // Burn | Fade | Shrink | None
        public double ItemDeathDuration     { get; set; } = 0.6;     // seconds

        // ── Sound ─────────────────────────────────────────────────────────────
        public bool   SoundEnabled          { get; set; } = true;
        public bool   UseDefaultSoundFolder { get; set; } = false;
        public string SoundNormalPath       { get; set; } = "";
        public string SoundSearchingPath    { get; set; } = "";
        public string SoundKillPath         { get; set; } = "";
        public string SoundBlockPath        { get; set; } = "";
        public double SoundVolume           { get; set; } = 0.8;
        public double SoundSpeed            { get; set; } = 1.0;

        // ── General ───────────────────────────────────────────────────────────
        public bool   IconAnimationsEnabled { get; set; } = true;
        public bool   GlobalNetLimitEnabled { get; set; } = false;
        public double GlobalNetLimitMb      { get; set; } = 100.0;
        public bool   RunAtStartup          { get; set; } = false;
    }
}
