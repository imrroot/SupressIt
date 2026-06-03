using System.Windows;
using System.Windows.Controls;
using SupressIt.Models;

namespace SupressIt.Views.Panels
{
    public partial class BlacklistPanel : UserControl
    {
        public event System.Action<int, bool> ToggleActiveRequested;
        public event System.Action<int>       RemoveRequested;

        public BlacklistPanel() => InitializeComponent();

        private void ToggleActive_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: int id, DataContext: BlacklistEntry entry })
                ToggleActiveRequested?.Invoke(id, !entry.IsActive);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: int id }) RemoveRequested?.Invoke(id);
        }
    }
}
