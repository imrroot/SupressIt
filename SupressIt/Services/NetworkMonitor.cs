using System; using System.Collections.Generic; using System.Diagnostics; using System.Linq;
using System.Net.NetworkInformation; using System.Runtime.InteropServices;
using SupressIt.Models;
namespace SupressIt.Services
{
    public class NetworkMonitor : INetworkMonitor
    {
        readonly Dictionary<int,(long r,long s)> _tot=new();
        Dictionary<int,(long r,long s)> _prev=new();
        (long r,long s) _nic=(0,0); bool _first=true;

        [DllImport("iphlpapi.dll")] static extern uint GetExtendedTcpTable(IntPtr p,ref int sz,bool o,int af,int cls,uint res);
        [StructLayout(LayoutKind.Sequential)] struct Row{public uint st,la,lp,ra,rp,pid;}

        public (Dictionary<int,long> down,Dictionary<int,long> up) Tick(){
            long nr=0,ns=0;
            try{foreach(var n in NetworkInterface.GetAllNetworkInterfaces()){if(n.OperationalStatus!=OperationalStatus.Up)continue;var s=n.GetIPv4Statistics();nr+=s.BytesReceived;ns+=s.BytesSent;}}catch{}
            long rd=_first?0:Math.Max(0,nr-_nic.r),sd=_first?0:Math.Max(0,ns-_nic.s);
            _nic=(nr,ns);_first=false;
            var pids=GetPidCounts(); int tot=pids.Values.DefaultIfEmpty(0).Sum();
            var snap=new Dictionary<int,(long,long)>();
            foreach(var kv in pids){double w=tot>0?(double)kv.Value/tot:0;
                _tot.TryGetValue(kv.Key,out var pv);
                var upd=(pv.r+(long)(rd*w),pv.s+(long)(sd*w));
                _tot[kv.Key]=upd;snap[kv.Key]=upd;}
            var dn=new Dictionary<int,long>();var un=new Dictionary<int,long>();
            foreach(var kv in snap){_prev.TryGetValue(kv.Key,out var pv);dn[kv.Key]=Math.Max(0,kv.Value.Item1-pv.r);un[kv.Key]=Math.Max(0,kv.Value.Item2-pv.s);}
            _prev=snap;return(dn,un);
        }

        public List<NetworkEntry> BuildEntries(string filter,Dictionary<int,long> dn,Dictionary<int,long> un){
            var low=filter?.ToLower()??""; var list=new List<NetworkEntry>();
            foreach(var kv in _tot){if(kv.Key<=0)continue;
                string name;try{name=Process.GetProcessById(kv.Key).ProcessName;}catch{name=$"PID {kv.Key}";}
                if(!string.IsNullOrEmpty(low)&&!name.ToLower().Contains(low))continue;
                dn.TryGetValue(kv.Key,out long d);un.TryGetValue(kv.Key,out long u);
                list.Add(new NetworkEntry{Pid=kv.Key,Name=name,DownSpeed=d,UpSpeed=u,TotalReceived=kv.Value.r,TotalSent=kv.Value.s});}
            return list.OrderByDescending(e=>e.TotalBytes).ToList();
        }

        public (long d,long u) GetTotals(Dictionary<int,long> dn,Dictionary<int,long> un){
            long d=0,u=0;foreach(var v in dn.Values)d+=v;foreach(var v in un.Values)u+=v;return(d,u);}

        Dictionary<int,int> GetPidCounts(){
            var c=new Dictionary<int,int>(); int sz=0;
            GetExtendedTcpTable(IntPtr.Zero,ref sz,true,2,5,0);
            var p=Marshal.AllocHGlobal(sz);
            try{if(GetExtendedTcpTable(p,ref sz,true,2,5,0)==0){
                int n=Marshal.ReadInt32(p);int rs=Marshal.SizeOf<Row>();
                for(int i=0;i<n;i++){var r=Marshal.PtrToStructure<Row>(p+4+i*rs);int pid=(int)r.pid;if(pid>0)c[pid]=c.GetValueOrDefault(pid)+1;}}}catch{}
            finally{Marshal.FreeHGlobal(p);}return c;
        }
    }
}
