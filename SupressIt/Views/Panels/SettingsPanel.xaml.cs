using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SupressIt.Views.Panels.Settings;

namespace SupressIt.Views.Panels
{
    public partial class SettingsPanel : UserControl
    {
        // Instantiate sub-views once; share DataContext with parent
        private readonly ThemeSettingsView _theme = new();
        private readonly GifSettingsView   _gif   = new();
        private readonly SoundSettingsView _sound = new();

        public SettingsPanel()
        {
            InitializeComponent();

            // Whenever THIS panel's DataContext changes, push it down to sub-views
            DataContextChanged += (_, _) =>
            {
                _theme.DataContext = DataContext;
                _gif.DataContext   = DataContext;
                _sound.DataContext = DataContext;
            };

            // Default visible child is Theme
            ChildContent.Content = _theme;
        }

        private void Child_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton btn) return;

            BtnTheme.IsChecked = false;
            BtnGif.IsChecked   = false;
            BtnSound.IsChecked = false;
            btn.IsChecked      = true;

            ChildContent.Content = btn.Tag?.ToString() switch
            {
                "Gif"   => (object)_gif,
                "Sound" => _sound,
                _       => _theme
            };
        }
    }
}
