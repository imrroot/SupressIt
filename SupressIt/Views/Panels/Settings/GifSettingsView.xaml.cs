using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SupressIt.ViewModels;

namespace SupressIt.Views.Panels.Settings
{
    public partial class GifSettingsView : UserControl
    {
        public GifSettingsView() => InitializeComponent();
        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string slot }) return;
            var vm = VM;
            if (vm?.UseDefaultGifFolder == true) return;

            var dlg = new OpenFileDialog
            {
                Title  = $"Pick animation for '{slot}' state",
                Filter = "GIF / Video|*.gif;*.webm;*.mp4;*.avi|All files|*.*"
            };
            if (dlg.ShowDialog() != true || vm == null) return;
            switch (slot)
            {
                case "normal":    vm.GifNormalPath    = dlg.FileName; break;
                case "searching": vm.GifSearchingPath = dlg.FileName; break;
                case "kill":      vm.GifKillPath      = dlg.FileName; break;
                case "block":     vm.GifBlockPath     = dlg.FileName; break;
            }
        }
    }
}
