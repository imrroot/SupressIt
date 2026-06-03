using System.ComponentModel;
namespace SupressIt.Models
{
    public class StartupEntry : INotifyPropertyChanged
    {
        private bool _en;
        public string Name      { get; set; } = "";
        public string Command   { get; set; } = "";
        public string Location  { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string SourceKind { get; set; } = "";
        public string SourceId   { get; set; } = "";
        public bool   IsToggleSupported { get; set; } = true;
        public bool   IsEnabled   { get => _en; set { _en = value; N(nameof(IsEnabled)); N(nameof(StatusLabel)); N(nameof(ToggleLabel)); } }
        public string StatusLabel => _en ? "ENABLED" : "DISABLED";
        public string ToggleLabel => _en ? "DISABLE" : "ENABLE";
        public event PropertyChangedEventHandler? PropertyChanged;
        void N(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
