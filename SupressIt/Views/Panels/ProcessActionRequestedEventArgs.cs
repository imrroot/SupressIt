using System.Windows;

namespace SupressIt.Views.Panels
{
    public sealed class ProcessActionRequestedEventArgs : System.EventArgs
    {
        public ProcessActionRequestedEventArgs(
            int processId,
            bool addToBlacklist,
            Point targetScreenPoint,
            FrameworkElement? targetElement)
        {
            ProcessId = processId;
            AddToBlacklist = addToBlacklist;
            TargetScreenPoint = targetScreenPoint;
            TargetElement = targetElement;
        }

        public int ProcessId { get; }
        public bool AddToBlacklist { get; }
        public Point TargetScreenPoint { get; }
        public FrameworkElement? TargetElement { get; }
    }
}
