using System.ComponentModel;
namespace SupressIt.Models
{
    public class NetworkEntry : INotifyPropertyChanged
    {
        private long _ds, _us, _tr, _ts; private bool _limEn; private long _limB = 10*1024*1024;
        public int    Pid  { get; set; }
        public string Name { get; set; }
        public long   DownSpeed     { get => _ds;  set { _ds  = value; N(nameof(DownSpeed));     } }
        public long   UpSpeed       { get => _us;  set { _us  = value; N(nameof(UpSpeed));       } }
        public long   TotalReceived { get => _tr;  set { _tr  = value; N(nameof(TotalReceived)); N(nameof(TotalBytes)); } }
        public long   TotalSent     { get => _ts;  set { _ts  = value; N(nameof(TotalSent));     N(nameof(TotalBytes)); } }
        public long   TotalBytes    => _tr + _ts;
        public bool   LimitEnabled  { get => _limEn; set { _limEn = value; N(nameof(LimitEnabled)); } }
        public long   LimitBytes    { get => _limB;  set { _limB  = value; N(nameof(LimitBytes)); N(nameof(LimitMb)); } }
        public double LimitMb       { get => _limB / 1_048_576.0; set { _limB = (long)(value * 1_048_576); N(nameof(LimitMb)); N(nameof(LimitBytes)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        void N(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
