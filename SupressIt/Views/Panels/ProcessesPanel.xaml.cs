using System.Windows;
using System.Windows.Controls;
using SupressIt.Models;

namespace SupressIt.Views.Panels
{
    public partial class ProcessesPanel : UserControl
    {
        public event System.EventHandler<ProcessActionRequestedEventArgs>? KillRequested;

        public ProcessesPanel() => InitializeComponent();

        private void Kill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not int pid) return;
            if (!ConfirmIfCritical(btn, "kill"))
                return;

            var card = FindCard(btn);
            var pt = GetTargetPoint(btn, card);

            KillRequested?.Invoke(
                this,
                new ProcessActionRequestedEventArgs(pid, false, pt, card));
        }

        private void Block_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not int pid) return;
            if (!ConfirmIfCritical(btn, "kill and blacklist"))
                return;

            var card = FindCard(btn);
            var pt = GetTargetPoint(btn, card);

            KillRequested?.Invoke(
                this,
                new ProcessActionRequestedEventArgs(pid, true, pt, card));
        }

        // Walk up visual tree to find the CardStyle Border
        private static FrameworkElement? FindCard(DependencyObject child)
        {
            var current = child;
            while (current != null)
            {
                if (current is Border b && b.CornerRadius.TopLeft >= 14)
                    return b;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static Point GetTargetPoint(Button button, FrameworkElement? card)
        {
            return card != null
                ? card.PointToScreen(new Point(card.ActualWidth / 2, card.ActualHeight / 2))
                : button.PointToScreen(new Point(18, 18));
        }

        private static bool ConfirmIfCritical(FrameworkElement source, string action)
        {
            if (source.DataContext is not ProcessEntry entry || !entry.HasCriticalWarning)
                return true;

            var result = MessageBox.Show(
                $"{entry.Name} is marked as critical.\n\n{entry.CriticalWarning}\n\nContinue and {action} it?",
                "Critical Windows Process",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }
    }
}
