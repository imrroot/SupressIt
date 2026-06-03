using System.Windows;
using System.Windows.Controls;
using SupressIt.Models;

namespace SupressIt.Views.Panels
{
    public partial class ServicesPanel : UserControl
    {
        public event System.Action<string> ToggleRequested;
        public event System.Action<string> BlockRequested;

        public ServicesPanel() => InitializeComponent();

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string name } button) return;
            if (!ConfirmIfCritical(button, "change this service"))
                return;

            ToggleRequested?.Invoke(name);
        }

        private void Block_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string name } button) return;
            if (!ConfirmIfCritical(button, "stop and blacklist this service"))
                return;

            BlockRequested?.Invoke(name);
        }

        private static bool ConfirmIfCritical(FrameworkElement source, string action)
        {
            if (source.DataContext is not ServiceEntry entry || !entry.HasCriticalWarning)
                return true;

            var result = MessageBox.Show(
                $"{entry.DisplayName} ({entry.ServiceName}) is marked as critical.\n\n{entry.CriticalWarning}\n\nContinue and {action}?",
                "Critical Windows Service",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }
    }
}
