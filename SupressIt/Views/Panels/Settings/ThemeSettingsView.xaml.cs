using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SupressIt.ViewModels;

namespace SupressIt.Views.Panels.Settings
{
    public partial class ThemeSettingsView : UserControl
    {
        public ThemeSettingsView() => InitializeComponent();

        private SettingsViewModel VM => DataContext as SettingsViewModel;

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string preset })
                VM?.ApplyAccentPreset(preset);
        }

        private void BrowseBg_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Pick background image or GIF",
                Filter = "Images & GIF|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.webm;*.mp4|All|*.*"
            };
            if (dlg.ShowDialog() == true && VM != null)
                VM.BackgroundPath = dlg.FileName;
        }
    }
}
