using System; using System.Collections.Generic; using System.Diagnostics; using System.Linq;
using System.ServiceProcess; using System.Threading; using System.Threading.Tasks;
using SupressIt.Models;
namespace SupressIt.Services
{
    public class BlacklistWatcher : IBlacklistWatcher
    {
        public event Action<string,string,string> EnforcementAction;
        readonly IBlacklistService _db; CancellationTokenSource _cts;
        public BlacklistWatcher(IBlacklistService db)=>_db=db;
        public void Start(){_cts=new CancellationTokenSource();Task.Run(()=>Loop(_cts.Token));}
        public void Stop()=>_cts?.Cancel();
        void Loop(CancellationToken ct){
            while(!ct.IsCancellationRequested){try{Enforce();}catch{}
                for(int i=0;i<30&&!ct.IsCancellationRequested;i++)Thread.Sleep(1000);}
        }
        void Enforce(){
            List<BlacklistEntry> list;try{list=_db.GetAll().Where(e=>e.IsActive).ToList();}catch{return;}
            foreach(var e in list){if(e.EntryType==BlacklistType.Process)KillProcs(e.Name);else StopSvc(e.Name);}
        }
        void KillProcs(string name){try{foreach(var p in Process.GetProcessesByName(name))try{int pid=p.Id;p.Kill();p.WaitForExit(2000);Fire("WATCH-KILL",name,$"re-killed PID {pid}");}catch(Exception ex){Fire("WATCH-KILL",name,$"FAILED — {ex.Message}");}}catch{}}
        void StopSvc(string name){try{using var sc=new ServiceController(name);if(sc.Status==ServiceControllerStatus.Running||sc.Status==ServiceControllerStatus.StartPending){sc.Stop();sc.WaitForStatus(ServiceControllerStatus.Stopped,TimeSpan.FromSeconds(10));Fire("WATCH-SVC",name,"re-stopped");}}catch{}}
        void Fire(string t,string n,string r)=>EnforcementAction?.Invoke(t,n,r);
    }
}
