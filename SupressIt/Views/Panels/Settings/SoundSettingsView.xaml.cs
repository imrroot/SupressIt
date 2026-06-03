using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SupressIt.ViewModels;

namespace SupressIt.Views.Panels.Settings
{
        public partial class SoundSettingsView : UserControl
    {
        public SoundSettingsView() => InitializeComponent();
        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string slot }) return;
            var vm = VM;
            if (vm?.UseDefaultSoundFolder == true) return;

            var dlg = new OpenFileDialog
            {
                Title  = $"Pick sound for '{slot}'",
                Filter = "Audio|*.mp3;*.wav;*.ogg;*.m4a;*.aac;*.flac;*.wma|All|*.*"
            };
            if (dlg.ShowDialog() != true || vm == null) return;
            switch (slot)
            {
                case "normal":    vm.SoundNormalPath    = dlg.FileName; break;
                case "searching": vm.SoundSearchingPath = dlg.FileName; break;
                case "kill":      vm.SoundKillPath      = dlg.FileName; break;
                case "block":     vm.SoundBlockPath     = dlg.FileName; break;
            }
        }
    }
}
