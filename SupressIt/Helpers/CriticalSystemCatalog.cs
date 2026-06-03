using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SupressIt.Helpers
{
    public static class CriticalSystemCatalog
    {
        private static readonly HashSet<string> CriticalProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "system",
            "registry",
            "smss",
            "csrss",
            "wininit",
            "winlogon",
            "services",
            "lsass",
            "lsaiso"
        };

        private static readonly Dictionary<string, string> CriticalServices = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DcomLaunch"] = "DCOM Server Process Launcher is a core Windows service.",
            ["EventLog"] = "Windows Event Log is required by core Windows components.",
            ["LSM"] = "Local Session Manager controls interactive Windows sessions.",
            ["PlugPlay"] = "Plug and Play is required for device and driver stability.",
            ["Power"] = "Power service is required for system power management.",
            ["ProfSvc"] = "User Profile Service is required for user logon sessions.",
            ["RpcEptMapper"] = "RPC Endpoint Mapper is required by core Windows services.",
            ["RpcSs"] = "Remote Procedure Call is required by Windows service control.",
            ["SamSs"] = "Security Accounts Manager is required by Windows security."
        };

        public static string GetProcessWarning(string processName, string path, IEnumerable<string>? serviceNames)
        {
            var normalizedName = NormalizeProcessName(processName);
            if (CriticalProcesses.Contains(normalizedName))
                return "Critical Windows process. Killing it can crash Windows or force a restart.";

            var criticalServices = serviceNames?
                .Where(name => CriticalServices.ContainsKey(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (criticalServices != null && criticalServices.Count > 0)
                return "Hosts critical service: " + string.Join(", ", criticalServices) + ". Stopping it can destabilize Windows.";

            return "";
        }

        public static string GetServiceWarning(string serviceName, string displayName)
        {
            if (CriticalServices.TryGetValue(serviceName ?? "", out var warning))
                return warning;

            return "";
        }

        private static string NormalizeProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return "";

            return Path.GetFileNameWithoutExtension(processName).Trim();
        }
    }
}
