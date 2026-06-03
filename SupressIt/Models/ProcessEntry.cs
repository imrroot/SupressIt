using System.ComponentModel;
namespace SupressIt.Models
{
    public class ProcessEntry : INotifyPropertyChanged
    {
        private double _cpu; private float _mem; private long _down, _up; private string _criticalWarning = "";
        public int    Pid       { get; set; }
        public string Name      { get; set; } = "";
        public string Path      { get; set; } = "";
        public bool   IsService { get; set; }
        public string CriticalWarning { get => _criticalWarning; set { _criticalWarning = value ?? ""; N(nameof(CriticalWarning)); N(nameof(HasCriticalWarning)); } }
        public bool   HasCriticalWarning => !string.IsNullOrWhiteSpace(_criticalWarning);
        public double CpuPercent    { get => _cpu;  set { _cpu  = value; N(nameof(CpuPercent));    } }
        public float  MemoryMb      { get => _mem;  set { _mem  = value; N(nameof(MemoryMb));      } }
        public long   DownloadSpeed { get => _down; set { _down = value; N(nameof(DownloadSpeed)); } }
        public long   UploadSpeed   { get => _up;   set { _up   = value; N(nameof(UploadSpeed));   } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void N(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
