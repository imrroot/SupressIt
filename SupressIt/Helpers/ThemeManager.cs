using System.Windows;
using System.Windows.Media;
using SupressIt.Models;

namespace SupressIt.Helpers
{
    /// <summary>
    /// Mutates existing (non-frozen) SolidColorBrush resources in App.Resources
    /// so every DynamicResource binding updates immediately without key replacement.
    /// </summary>
    public static class ThemeManager
    {
        public static void Apply(AppSettings s)
        {
            var r = Application.Current.Resources;

            SetBrush(r, "BgShell",       s.ColorShellBg);
            SetBrush(r, "BgCard",        s.ColorCardBg);
            SetBrush(r, "BgTabBar",      s.ColorTabBarBg);
            SetBrush(r, "TextPrimary",   s.ColorTextPrimary);
            SetBrush(r, "TextSecond",    s.ColorTextSecond);
            SetBrush(r, "TextSoft",      s.ColorTextSoft);
            SetBrush(r, "AccentBrush",   s.ColorAccent);
            SetBrush(r, "AccentBrush2",  s.ColorAccent2);
            SetBrush(r, "SoftBtnFg",     s.ColorAccent);
            SetBrush(r, "LogTextBrush",  s.ColorLogText);
            SetBrush(r, "LogTagBrush",   s.ColorLogTag);
            SetBrush(r, "AnimePanelBg",  s.ColorAnimePanelBg);

            // Kill / Block button backgrounds (with alpha)
            var killC  = Parse(s.ColorKillBtn,  Color.FromRgb(0xFF, 0x40, 0x60));
            var blockC = Parse(s.ColorBlockBtn, Color.FromRgb(0x80, 0x50, 0xC0));
            SetBrushDirect(r, "KillBtnBg",   Color.FromArgb(0x30, killC.R,  killC.G,  killC.B));
            SetBrushDirect(r, "KillBtnHover",Color.FromArgb(0x55, killC.R,  killC.G,  killC.B));
            SetBrushDirect(r, "SoftBtnBg",   Color.FromArgb(0x28, blockC.R, blockC.G, blockC.B));
            SetBrushDirect(r, "AccentTabChecked", Parse(s.ColorTabChecked, Color.FromArgb(0x30,0x80,0x50,0xC0)));
            SetBrushDirect(r, "AccentTabHover",   Parse(s.ColorTabHover,  Color.FromArgb(0x18,0x80,0x50,0xC0)));

            // Log gradient colors
            var logBg = Parse(s.ColorLogBg, Color.FromRgb(0x18, 0x0A, 0x2A));
            r["LogBg1"] = logBg;
            r["LogBg2"] = Color.FromRgb(
                (byte)(logBg.R / 2), (byte)(logBg.G / 2), (byte)(logBg.B / 2));

            // Anime panel gradient
            var apBg = Parse(s.ColorAnimePanelBg, Color.FromRgb(0x20, 0x14, 0x36));
            r["AnimePanelBg1"] = apBg;
            r["AnimePanelBg2"] = Color.FromRgb(
                (byte)(apBg.R * 3 / 4), (byte)(apBg.G * 3 / 4), (byte)(apBg.B * 3 / 4));
        }

        // ── Dark / Light presets ──────────────────────────────────────────────

        public static void ApplyDarkPreset(AppSettings s)
        {
            s.ColorShellBg      = "#140E22";
            s.ColorCardBg       = "#221838";
            s.ColorTabBarBg     = "#181028";
            s.ColorTextPrimary  = "#E8D8FF";
            s.ColorTextSecond   = "#A088C8";
            s.ColorTextSoft     = "#605080";
            s.ColorLogBg        = "#180A2A";
            s.ColorAnimePanelBg = "#201436";
        }

        public static void ApplyLightPreset(AppSettings s)
        {
            s.ColorShellBg      = "#FEF6FF";
            s.ColorCardBg       = "#FFFFFF";
            s.ColorTabBarBg     = "#F8F0FF";
            s.ColorTextPrimary  = "#3D2050";
            s.ColorTextSecond   = "#907090";
            s.ColorTextSoft     = "#C0A0D8";
            s.ColorLogBg        = "#271540";
            s.ColorAnimePanelBg = "#FFF0FF";
        }

        // ── Accent palette presets ────────────────────────────────────────────

        public static void ApplyAccentPreset(AppSettings s, string name)
        {
            switch (name)
            {
                case "Purple": s.ColorAccent="#8050C0"; s.ColorAccent2="#C050A0"; s.ColorKillBtn="#FF4060"; s.ColorBlockBtn="#8050C0"; break;
                case "Blue":   s.ColorAccent="#3070FF"; s.ColorAccent2="#00C8FF"; s.ColorKillBtn="#FF4060"; s.ColorBlockBtn="#3070FF"; break;
                case "Pink":   s.ColorAccent="#FF5090"; s.ColorAccent2="#FF90C0"; s.ColorKillBtn="#FF3060"; s.ColorBlockBtn="#FF5090"; break;
                case "Mint":   s.ColorAccent="#20C080"; s.ColorAccent2="#00F0A0"; s.ColorKillBtn="#FF5040"; s.ColorBlockBtn="#20C080"; break;
                case "Orange": s.ColorAccent="#FF8020"; s.ColorAccent2="#FFB040"; s.ColorKillBtn="#FF4020"; s.ColorBlockBtn="#FF8020"; break;
                case "Red":    s.ColorAccent="#E03030"; s.ColorAccent2="#FF6060"; s.ColorKillBtn="#FF2020"; s.ColorBlockBtn="#E03030"; break;
                case "Cyber":  s.ColorAccent="#00FFB0"; s.ColorAccent2="#00C8FF"; s.ColorKillBtn="#FF0060"; s.ColorBlockBtn="#00FFB0"; break;
                case "Sakura": s.ColorAccent="#FF80A0"; s.ColorAccent2="#FFB0C8"; s.ColorKillBtn="#FF5070"; s.ColorBlockBtn="#FF80A0"; break;
            }
            s.ColorTabChecked = ToArgbHex(0x30, Parse(s.ColorAccent, Color.FromRgb(0x80,0x50,0xC0)));
            s.ColorTabHover   = ToArgbHex(0x18, Parse(s.ColorAccent, Color.FromRgb(0x80,0x50,0xC0)));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetBrush(ResourceDictionary r, string key, string hex)
        {
            var color = Parse(hex, Colors.Magenta);
            SetBrushDirect(r, key, color);
        }

        private static void SetBrushDirect(ResourceDictionary r, string key, Color color)
        {
            if (r[key] is SolidColorBrush existing && !existing.IsFrozen)
                existing.Color = color;
            else
                r[key] = new SolidColorBrush(color);
        }

        public static Color Parse(string hex, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return fallback; }
        }

        private static string ToArgbHex(byte a, Color c) =>
            $"#{a:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
