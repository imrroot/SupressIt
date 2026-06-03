using System.Windows;
using System.Windows.Controls;
using SupressIt.Models;

namespace SupressIt.Views.Panels
{
    public partial class StartupPanel : UserControl
    {
        public event System.Action<StartupEntry> ToggleRequested;
        public StartupPanel() => InitializeComponent();
        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: StartupEntry entry }) ToggleRequested?.Invoke(entry);
        }
    }
}
