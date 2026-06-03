using System.Windows;

namespace SupressIt.Views.Panels
{
    public sealed class NetworkBlockRequestedEventArgs : System.EventArgs
    {
        public NetworkBlockRequestedEventArgs(
            string processName,
            Point targetScreenPoint,
            FrameworkElement? targetElement)
        {
            ProcessName = processName;
            TargetScreenPoint = targetScreenPoint;
            TargetElement = targetElement;
        }

        public string ProcessName { get; }
        public Point TargetScreenPoint { get; }
        public FrameworkElement? TargetElement { get; }
    }
}
