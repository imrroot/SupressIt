using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SupressIt.Views.Panels.Settings
{
    public partial class ColorRow : UserControl
    {
        // ── Label DP ──────────────────────────────────────────────────────────
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(ColorRow),
                new PropertyMetadata(""));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        // ── Hex DP ────────────────────────────────────────────────────────────
        public static readonly DependencyProperty HexProperty =
            DependencyProperty.Register(nameof(Hex), typeof(string), typeof(ColorRow),
                new FrameworkPropertyMetadata("",
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnHexChanged));

        public string Hex
        {
            get => (string)GetValue(HexProperty);
            set => SetValue(HexProperty, value);
        }

        // Called when the DP changes (either from binding push-down OR from textbox)
        private static void OnHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorRow row)
                row.UpdateSwatch(e.NewValue as string);
        }

        public ColorRow()
        {
            InitializeComponent();
            // Sync swatch after template is applied
            Loaded += (_, _) => UpdateSwatch(Hex);
        }

        // ── FIX 1: live update on every keystroke ─────────────────────────────
        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update swatch immediately while typing
            UpdateSwatch(HexBox.Text);

            // Push value back to DP (and therefore to the binding target = SettingsViewModel)
            // only when the text is a valid colour so we don't spam invalid values
            if (IsValidHex(HexBox.Text))
                Hex = HexBox.Text;
        }

        private void UpdateSwatch(string hex)
        {
            if (Swatch == null) return;
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex ?? "#00000000");
                Swatch.Fill = new SolidColorBrush(c);
            }
            catch
            {
                // Invalid mid-typing — just leave swatch as-is, don't crash
            }
        }

        private static bool IsValidHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return false;
            try { ColorConverter.ConvertFromString(hex); return true; }
            catch { return false; }
        }
    }
}
