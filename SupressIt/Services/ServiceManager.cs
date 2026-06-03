using System; using System.Collections.Generic; using System.Linq; using System.ServiceProcess;
using SupressIt.Helpers;
using SupressIt.Models;
namespace SupressIt.Services
{
    public class ServiceManager : IWindowsServiceManager
    {
        public List<ServiceEntry> GetServices(string filter){
            var low=filter?.ToLowerInvariant()??"";
            try{return ServiceController.GetServices()
                .Where(s=>string.IsNullOrEmpty(low)||s.ServiceName.ToLower().Contains(low)||s.DisplayName.ToLower().Contains(low))
                .OrderBy(s=>s.ServiceName)
                .Select(s=>{string st="Unknown";try{st=s.StartType.ToString();}catch{}
                    return new ServiceEntry{ServiceName=s.ServiceName,DisplayName=s.DisplayName,Status=s.Status.ToString(),StartType=st,
                        CriticalWarning=CriticalSystemCatalog.GetServiceWarning(s.ServiceName,s.DisplayName)};})
                .ToList();}catch{return new List<ServiceEntry>();}
        }
        public (string status,string result) Toggle(string name){
            try{using var sc=new ServiceController(name);
                if(sc.Status==ServiceControllerStatus.Running){sc.Stop();sc.WaitForStatus(ServiceControllerStatus.Stopped,TimeSpan.FromSeconds(10));return(sc.Status.ToString(),"stopped");}
                sc.Start();sc.WaitForStatus(ServiceControllerStatus.Running,TimeSpan.FromSeconds(10));return(sc.Status.ToString(),"started");}
            catch(Exception ex){return("Unknown",$"FAILED — {ex.Message}");}
        }
        public string StopOnly(string name){
            try{using var sc=new ServiceController(name);
                if(sc.Status==ServiceControllerStatus.Running||sc.Status==ServiceControllerStatus.StartPending){
                    sc.Stop();sc.WaitForStatus(ServiceControllerStatus.Stopped,TimeSpan.FromSeconds(10));return"stopped";}
                return"already stopped";}
            catch(Exception ex){return$"FAILED — {ex.Message}";}
        }
    }
}
