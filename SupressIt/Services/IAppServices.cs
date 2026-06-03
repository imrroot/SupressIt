using System;
using System.Collections.Generic;
using SupressIt.Models;

namespace SupressIt.Services
{
    public interface IProcessService
    {
        List<ProcessEntry> GetProcesses(string filter, Dictionary<int, long> down, Dictionary<int, long> up);
        (string name, string result) Kill(int pid);
    }

    public interface IWindowsServiceManager
    {
        List<ServiceEntry> GetServices(string filter);
        (string status, string result) Toggle(string name);
        string StopOnly(string name);
    }

    public interface INetworkMonitor
    {
        (Dictionary<int, long> down, Dictionary<int, long> up) Tick();
        List<NetworkEntry> BuildEntries(string filter, Dictionary<int, long> down, Dictionary<int, long> up);
        (long d, long u) GetTotals(Dictionary<int, long> down, Dictionary<int, long> up);
    }

    public interface IVpnDetector
    {
        VpnStatus Check();
    }

    public interface IStartupService
    {
        List<StartupEntry> GetStartupEntries(string filter = "");
        bool Toggle(StartupEntry entry);
    }

    public interface IBlacklistService
    {
        List<BlacklistEntry> GetAll();
        BlacklistEntry? Add(string name, BlacklistType type);
        void Remove(int id);
        void SetActive(int id, bool active);
        bool IsBlacklisted(string name, BlacklistType type);
    }

    public interface IBlacklistWatcher
    {
        event Action<string, string, string> EnforcementAction;
        void Start();
        void Stop();
    }
}
