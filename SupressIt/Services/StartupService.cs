using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using Microsoft.Win32;
using SupressIt.Models;

namespace SupressIt.Services
{
    public class StartupService : IStartupService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunOnceKey = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string MachineRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string MachineRunOnceKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string ApprovedRunKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string ApprovedRun32Key = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
        private const string ApprovedStartupFolderKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

        public List<StartupEntry> GetStartupEntries(string filter = "")
        {
            var entries = new List<StartupEntry>();

            ReadRegistryEntries(entries);
            ReadStartupFolders(entries);
            ReadScheduledTasks(entries);
            ReadWmiStartupCommands(entries);

            var unique = entries
                .GroupBy(entry => CreateDuplicateKey(entry), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var low = filter?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(low))
            {
                unique = unique
                    .Where(entry =>
                        (entry.Name ?? "").ToLowerInvariant().Contains(low) ||
                        (entry.Command ?? "").ToLowerInvariant().Contains(low) ||
                        (entry.Location ?? "").ToLowerInvariant().Contains(low))
                    .ToList();
            }

            return unique
                .OrderBy(entry => entry.Name)
                .ThenBy(entry => entry.Location)
                .ToList();
        }

        public bool Toggle(StartupEntry entry)
        {
            if (entry == null || !entry.IsToggleSupported)
                return entry?.IsEnabled ?? false;

            var targetEnabled = !entry.IsEnabled;
            var changed = entry.SourceKind switch
            {
                "Registry" => SetStartupApproved(entry.SourceId, targetEnabled),
                "StartupFolder" => SetStartupApproved(entry.SourceId, targetEnabled),
                "ScheduledTask" => SetScheduledTaskEnabled(entry.SourceId, targetEnabled),
                _ => false
            };

            if (changed)
                entry.IsEnabled = targetEnabled;

            return entry.IsEnabled;
        }

        private static void ReadRegistryEntries(List<StartupEntry> target)
        {
            ReadRegistryView(target, RegistryHive.CurrentUser, RegistryView.Default, RegistryView.Default, RunKey, ApprovedRunKey, "HKCU Run");
            ReadRegistryView(target, RegistryHive.CurrentUser, RegistryView.Default, RegistryView.Default, RunOnceKey, ApprovedRunKey, "HKCU RunOnce");

            ReadRegistryView(target, RegistryHive.LocalMachine, RegistryView.Registry64, RegistryView.Registry64, MachineRunKey, ApprovedRunKey, "HKLM Run");
            ReadRegistryView(target, RegistryHive.LocalMachine, RegistryView.Registry64, RegistryView.Registry64, MachineRunOnceKey, ApprovedRunKey, "HKLM RunOnce");
            ReadRegistryView(target, RegistryHive.LocalMachine, RegistryView.Registry32, RegistryView.Registry64, MachineRunKey, ApprovedRun32Key, "HKLM Run 32-bit");
            ReadRegistryView(target, RegistryHive.LocalMachine, RegistryView.Registry32, RegistryView.Registry64, MachineRunOnceKey, ApprovedRun32Key, "HKLM RunOnce 32-bit");
        }

        private static void ReadRegistryView(
            List<StartupEntry> target,
            RegistryHive hive,
            RegistryView view,
            RegistryView approvedView,
            string runKey,
            string approvedKey,
            string location)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey(runKey);
                if (key == null)
                    return;

                using var approvedBaseKey = RegistryKey.OpenBaseKey(hive, approvedView);
                var disabled = Disabled(approvedBaseKey, approvedKey);
                foreach (var name in key.GetValueNames())
                {
                    var command = key.GetValue(name)?.ToString() ?? "";
                    target.Add(new StartupEntry
                    {
                        Name = name,
                        Command = command,
                        Location = location,
                        SourceKind = "Registry",
                        SourceId = BuildRegistrySourceId(hive, approvedView, approvedKey, name),
                        IsEnabled = !disabled.Contains(name)
                    });
                }
            }
            catch
            {
            }
        }

        private static void ReadStartupFolders(List<StartupEntry> target)
        {
            ReadStartupFolder(
                target,
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                RegistryHive.CurrentUser,
                RegistryView.Default,
                "User Startup");

            ReadStartupFolder(
                target,
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                RegistryHive.LocalMachine,
                RegistryView.Registry64,
                "Common Startup");
        }

        private static void ReadStartupFolder(
            List<StartupEntry> target,
            string folder,
            RegistryHive hive,
            RegistryView view,
            string location)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                    return;

                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                var disabled = Disabled(baseKey, ApprovedStartupFolderKey);

                foreach (var file in Directory.GetFiles(folder))
                {
                    var extension = Path.GetExtension(file);
                    if (!IsStartupFolderFile(extension))
                        continue;

                    var valueName = Path.GetFileName(file);
                    target.Add(new StartupEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Command = file,
                        Location = location,
                        SourceKind = "StartupFolder",
                        SourceId = BuildRegistrySourceId(hive, view, ApprovedStartupFolderKey, valueName),
                        IsEnabled = !disabled.Contains(valueName)
                    });
                }
            }
            catch
            {
            }
        }

        private static void ReadScheduledTasks(List<StartupEntry> target)
        {
            try
            {
                var type = Type.GetTypeFromProgID("Schedule.Service");
                if (type == null)
                    return;

                dynamic scheduler = Activator.CreateInstance(type);
                if (scheduler == null)
                    return;

                scheduler.Connect();
                ReadTaskFolder(scheduler.GetFolder("\\"), target);
            }
            catch
            {
            }
        }

        private static void ReadTaskFolder(dynamic folder, List<StartupEntry> target)
        {
            try
            {
                foreach (dynamic task in folder.GetTasks(0))
                    AddTaskIfStartup(task, target);

                foreach (dynamic child in folder.GetFolders(0))
                    ReadTaskFolder(child, target);
            }
            catch
            {
            }
        }

        private static void AddTaskIfStartup(dynamic task, List<StartupEntry> target)
        {
            try
            {
                string path = task.Path;
                if (path.StartsWith(@"\Microsoft\Windows\", StringComparison.OrdinalIgnoreCase))
                    return;

                dynamic definition = task.Definition;
                if (IsHiddenTask(definition) || !HasStartupTrigger(definition))
                    return;

                var command = GetFirstExecAction(definition);
                if (string.IsNullOrWhiteSpace(command))
                    return;

                target.Add(new StartupEntry
                {
                    Name = task.Name,
                    Command = command,
                    Location = "Scheduled Task",
                    SourceKind = "ScheduledTask",
                    SourceId = path,
                    IsEnabled = task.Enabled
                });
            }
            catch
            {
            }
        }

        private static bool IsHiddenTask(dynamic definition)
        {
            try { return definition.Settings.Hidden; }
            catch { return false; }
        }

        private static bool HasStartupTrigger(dynamic definition)
        {
            try
            {
                foreach (dynamic trigger in definition.Triggers)
                {
                    int type = trigger.Type;
                    if (type == 8 || type == 9)
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static string GetFirstExecAction(dynamic definition)
        {
            try
            {
                foreach (dynamic action in definition.Actions)
                {
                    int type = action.Type;
                    if (type != 0)
                        continue;

                    var path = action.Path as string ?? "";
                    var args = action.Arguments as string ?? "";
                    return string.IsNullOrWhiteSpace(args) ? path : $"{path} {args}";
                }
            }
            catch
            {
            }

            return "";
        }

        private static bool SetStartupApproved(string sourceId, bool enabled)
        {
            try
            {
                var parts = sourceId.Split('|');
                if (parts.Length != 4)
                    return false;

                var hive = Enum.Parse<RegistryHive>(parts[0]);
                var view = Enum.Parse<RegistryView>(parts[1]);
                var subKey = parts[2];
                var valueName = parts[3];

                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey(subKey, true) ?? baseKey.CreateSubKey(subKey);
                var data = new byte[12];
                data[0] = enabled ? (byte)0x02 : (byte)0x03;
                key.SetValue(valueName, data, RegistryValueKind.Binary);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ReadWmiStartupCommands(List<StartupEntry> target)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, Command, Location, User FROM Win32_StartupCommand");

                foreach (ManagementObject item in searcher.Get())
                {
                    var name = item["Name"]?.ToString() ?? "";
                    var command = item["Command"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(command))
                        continue;

                    var location = item["Location"]?.ToString() ?? "Windows Startup";
                    var user = item["User"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(user))
                        location = $"{location} - {user}";

                    target.Add(new StartupEntry
                    {
                        Name = string.IsNullOrWhiteSpace(name) ? command : name,
                        Command = command,
                        Location = location,
                        SourceKind = "WmiStartupCommand",
                        SourceId = command,
                        IsEnabled = true,
                        IsToggleSupported = false
                    });
                }
            }
            catch
            {
            }
        }

        private static bool SetScheduledTaskEnabled(string taskPath, bool enabled)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Change /TN \"{taskPath}\" {(enabled ? "/Enable" : "/Disable")}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return false;

                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static HashSet<string> Disabled(RegistryKey root, string subKey)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var key = root.OpenSubKey(subKey);
                if (key == null)
                    return set;

                foreach (var name in key.GetValueNames())
                {
                    var data = key.GetValue(name) as byte[];
                    if (data != null && data.Length > 0 && data[0] == 0x03)
                        set.Add(name);
                }
            }
            catch
            {
            }

            return set;
        }

        private static bool IsStartupFolderFile(string extension)
        {
            return extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".url", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".bat", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildRegistrySourceId(RegistryHive hive, RegistryView view, string approvedKey, string valueName)
        {
            return $"{hive}|{view}|{approvedKey}|{valueName}";
        }

        private static string CreateDuplicateKey(StartupEntry entry)
        {
            return string.IsNullOrWhiteSpace(entry.Command)
                ? $"{entry.SourceKind}:{entry.Name}:{entry.Location}"
                : entry.Command.Trim();
        }
    }
}
