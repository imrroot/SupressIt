using System.ComponentModel;
namespace SupressIt.Models
{
    public enum BlacklistType { Process, Service }
    public class BlacklistEntry : INotifyPropertyChanged
    {
        private bool _active;
        public int           Id        { get; set; }
        public string        Name      { get; set; }
        public BlacklistType EntryType { get; set; }
        public string        AddedAt   { get; set; }
        public bool   IsActive          { get => _active; set { _active = value; N(nameof(IsActive)); N(nameof(ActiveLabel)); N(nameof(ToggleActiveLabel)); } }
        public string TypeLabel         => EntryType == BlacklistType.Service ? "SERVICE" : "PROCESS";
        public string ActiveLabel       => _active ? "ENFORCING" : "PAUSED";
        public string ToggleActiveLabel => _active ? "PAUSE"     : "RESUME";
        public event PropertyChangedEventHandler PropertyChanged;
        void N(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
