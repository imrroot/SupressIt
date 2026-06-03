using System; using System.Collections.Generic; using System.Diagnostics; using System.Linq;
using System.Management; using SupressIt.Helpers; using SupressIt.Models;
namespace SupressIt.Services
{
    public class ProcessService : IProcessService
    {
        readonly Dictionary<int,TimeSpan> _prevT=new();
        readonly Dictionary<int,DateTime> _prevD=new();
        HashSet<int> _svcPids=new();
        Dictionary<int,List<string>> _svcNamesByPid=new();

        public IReadOnlySet<int> ServicePids=>_svcPids;

        public List<ProcessEntry> GetProcesses(string filter,Dictionary<int,long> down,Dictionary<int,long> up)
        {
            RefreshServicePids();
            var now=DateTime.UtcNow; var low=filter?.ToLowerInvariant()??"";
            var result=new List<ProcessEntry>();
            foreach(var p in Process.GetProcesses())
            {
                try{
                    if(p.Id==0)continue;
                    if(!string.IsNullOrEmpty(low)&&!p.ProcessName.ToLowerInvariant().Contains(low))continue;
                    double cpu=CalcCpu(p,now); float mem=p.WorkingSet64/1_048_576f;
                    string path=""; try{path=p.MainModule?.FileName??"";}catch{}
                    down.TryGetValue(p.Id,out long d); up.TryGetValue(p.Id,out long u);
                    _svcNamesByPid.TryGetValue(p.Id,out var serviceNames);
                    serviceNames ??= new List<string>();
                    result.Add(new ProcessEntry{Pid=p.Id,Name=p.ProcessName,Path=path,IsService=_svcPids.Contains(p.Id),
                        CpuPercent=Math.Round(cpu,1),MemoryMb=mem,DownloadSpeed=d,UploadSpeed=u,
                        CriticalWarning=CriticalSystemCatalog.GetProcessWarning(p.ProcessName,path,serviceNames)});
                }catch{}
            }
            return result.OrderBy(e=>e.Name).ToList();
        }

        public (string name,string result) Kill(int pid){
            string name="";
            try{var p=Process.GetProcessById(pid);name=p.ProcessName;p.Kill();p.WaitForExit(3000);return(name,"killed OK");}
            catch(Exception ex){return(name,$"FAILED — {ex.Message}");}
        }

        double CalcCpu(Process p,DateTime now){
            try{var el=p.TotalProcessorTime;
                if(_prevT.TryGetValue(p.Id,out var pc)&&_prevD.TryGetValue(p.Id,out var pd)){
                    double ms=(now-pd).TotalMilliseconds;
                    if(ms>0){_prevT[p.Id]=el;_prevD[p.Id]=now;return(el-pc).TotalMilliseconds/(ms*Environment.ProcessorCount)*100;}}
                _prevT[p.Id]=el;_prevD[p.Id]=now;}catch{}return 0;
        }

        void RefreshServicePids(){
            var s=new HashSet<int>();
            var names=new Dictionary<int,List<string>>();
            try{using var q=new ManagementObjectSearcher("SELECT Name, ProcessId FROM Win32_Service");
                foreach(ManagementObject o in q.Get())try{
                    var pid=Convert.ToInt32(o["ProcessId"]);
                    if(pid<=0)continue;
                    s.Add(pid);
                    if(!names.TryGetValue(pid,out var list)){list=new List<string>();names[pid]=list;}
                    list.Add(o["Name"]?.ToString()??"");
                }catch{}}catch{}
            _svcPids=s;
            _svcNamesByPid=names;
        }
    }
}
