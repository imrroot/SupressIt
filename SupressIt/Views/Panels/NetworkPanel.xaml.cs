using System.Windows;
using System.Windows.Controls;

namespace SupressIt.Views.Panels
{
    public partial class NetworkPanel : UserControl
    {
        public event System.EventHandler<NetworkBlockRequestedEventArgs>? BlockRequested;

        public NetworkPanel() => InitializeComponent();

        private void Block_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string name }) return;

            var row = FindCard((DependencyObject)sender);
            var target = row != null
                ? row.PointToScreen(new Point(row.ActualWidth / 2, row.ActualHeight / 2))
                : ((Button)sender).PointToScreen(new Point(18, 18));

            BlockRequested?.Invoke(
                this,
                new NetworkBlockRequestedEventArgs(name, target, row));
        }

        private static FrameworkElement? FindCard(DependencyObject child)
        {
            var cur = child;
            while (cur != null)
            {
                if (cur is Border b && b.CornerRadius.TopLeft >= 14)
                    return b;
                cur = System.Windows.Media.VisualTreeHelper.GetParent(cur);
            }
            return null;
        }
    }
}
