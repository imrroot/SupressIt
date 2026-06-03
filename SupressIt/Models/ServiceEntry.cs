using System.ComponentModel;
namespace SupressIt.Models
{
    public class ServiceEntry : INotifyPropertyChanged
    {
        private string _status = "";
        private string _criticalWarning = "";
        public string ServiceName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string StartType   { get; set; } = "";
        public string CriticalWarning { get => _criticalWarning; set { _criticalWarning = value ?? ""; N(nameof(CriticalWarning)); N(nameof(HasCriticalWarning)); } }
        public bool   HasCriticalWarning => !string.IsNullOrWhiteSpace(_criticalWarning);
        public string Status      { get => _status; set { _status = value; N(nameof(Status)); N(nameof(ActionLabel)); } }
        public string ActionLabel => _status == "Running" ? "STOP" : "START";
        public event PropertyChangedEventHandler? PropertyChanged;
        void N(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
