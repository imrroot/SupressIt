using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SupressIt.Models;
using SupressIt.Services;

namespace SupressIt.ViewModels
{
    public enum ActiveTab { Processes, Services, Network, Startup, Blacklist, Settings }
    public enum AnimeState { Normal, Searching, Killing, Blocking }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IProcessService _processesService;
        private readonly IWindowsServiceManager _servicesService;
        private readonly INetworkMonitor _networkMonitor;
        private readonly IVpnDetector _vpnDetector;
        private readonly IStartupService _startupService;
        private readonly IBlacklistService _blacklistService;
        private readonly IBlacklistWatcher _blacklistWatcher;
        private readonly DispatcherTimer _mainTimer;
        private readonly DispatcherTimer _vpnTimer;
        private readonly DispatcherTimer _searchTimer;

        private Dictionary<int, long> _downloadSpeeds = new();
        private Dictionary<int, long> _uploadSpeeds = new();
        private ActiveTab _activeTab = ActiveTab.Processes;
        private string _pendingSearch = "";
        private string _appliedSearch = "";
        private string _statusText = "0 items";
        private string _totalDown = "0 B/s";
        private string _totalUp = "0 B/s";
        private bool _vpnActive;
        private string _vpnLabel = "SCANNING...";
        private int _killCount;
        private AnimeState _animeState = AnimeState.Normal;
        private bool _globalNetLimitEnabled;
        private double _globalNetLimitMb = 100;
        private int _actionVersion;

        public ObservableCollection<ProcessEntry> Processes { get; } = new();
        public ObservableCollection<ServiceEntry> Services { get; } = new();
        public ObservableCollection<NetworkEntry> Network { get; } = new();
        public ObservableCollection<StartupEntry> Startup { get; } = new();
        public ObservableCollection<BlacklistEntry> Blacklist { get; } = new();
        public ObservableCollection<KillLogEntry> KillLog { get; } = new();

        public ActiveTab ActiveTab
        {
            get => _activeTab;
            set
            {
                if (_activeTab == value) return;
                _activeTab = value;
                OnPropertyChanged(nameof(ActiveTab));
                Refresh();
            }
        }

        public string SearchText
        {
            get => _pendingSearch;
            set
            {
                value ??= "";
                if (_pendingSearch == value) return;

                _pendingSearch = value;
                OnPropertyChanged(nameof(SearchText));
                AnimeState = value.Length > 0 ? AnimeState.Searching : AnimeState.Normal;
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set => Set(ref _statusText, value, nameof(StatusText));
        }

        public string TotalDown
        {
            get => _totalDown;
            private set => Set(ref _totalDown, value, nameof(TotalDown));
        }

        public string TotalUp
        {
            get => _totalUp;
            private set => Set(ref _totalUp, value, nameof(TotalUp));
        }

        public bool VpnActive
        {
            get => _vpnActive;
            private set => Set(ref _vpnActive, value, nameof(VpnActive));
        }

        public string VpnLabel
        {
            get => _vpnLabel;
            private set => Set(ref _vpnLabel, value, nameof(VpnLabel));
        }

        public int KillCount
        {
            get => _killCount;
            private set => Set(ref _killCount, value, nameof(KillCount));
        }

        public AnimeState AnimeState
        {
            get => _animeState;
            set
            {
                if (_animeState == value) return;
                _animeState = value;
                OnPropertyChanged(nameof(AnimeState));
                AnimeStateChanged?.Invoke(value);
            }
        }

        public bool GlobalNetLimitEnabled
        {
            get => _globalNetLimitEnabled;
            set => Set(ref _globalNetLimitEnabled, value, nameof(GlobalNetLimitEnabled));
        }

        public double GlobalNetLimitMb
        {
            get => _globalNetLimitMb;
            set => Set(ref _globalNetLimitMb, value, nameof(GlobalNetLimitMb));
        }

        public event Action? KillLogUpdated;
        public event Action<AnimeState>? AnimeStateChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
            : this(
                new ProcessService(),
                new ServiceManager(),
                new NetworkMonitor(),
                new VpnDetector(),
                new StartupService(),
                new BlacklistService())
        {
        }

        public MainViewModel(
            IProcessService processesService,
            IWindowsServiceManager servicesService,
            INetworkMonitor networkMonitor,
            IVpnDetector vpnDetector,
            IStartupService startupService,
            IBlacklistService blacklistService,
            IBlacklistWatcher? blacklistWatcher = null)
        {
            _processesService = processesService;
            _servicesService = servicesService;
            _networkMonitor = networkMonitor;
            _vpnDetector = vpnDetector;
            _startupService = startupService;
            _blacklistService = blacklistService;
            _blacklistWatcher = blacklistWatcher ?? new BlacklistWatcher(_blacklistService);

            _blacklistWatcher.EnforcementAction += (type, name, result) =>
                InvokeOnUi(() => Log(type, "auto", name, result));
            _blacklistWatcher.Start();

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _searchTimer.Tick += (_, _) => ApplySearch();

            _mainTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _mainTimer.Tick += (_, _) => Tick();

            _vpnTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _vpnTimer.Tick += (_, _) => CheckVpn();
            _vpnTimer.Start();

            LoadInitialProcesses();
        }

        public void Shutdown()
        {
            _mainTimer.Stop();
            _vpnTimer.Stop();
            _searchTimer.Stop();
            _blacklistWatcher.Stop();
        }

        public int BeginDestructiveAction(bool blacklist)
        {
            var version = ++_actionVersion;
            var state = blacklist ? AnimeState.Blocking : AnimeState.Killing;

            if (_animeState == state)
                AnimeStateChanged?.Invoke(state);
            else
                AnimeState = state;

            return version;
        }

        public void Refresh()
        {
            switch (_activeTab)
            {
                case ActiveTab.Processes:
                    RefreshProcesses();
                    break;
                case ActiveTab.Services:
                    RefreshServices();
                    break;
                case ActiveTab.Network:
                    RefreshNetwork();
                    break;
                case ActiveTab.Startup:
                    RefreshStartup();
                    break;
                case ActiveTab.Blacklist:
                    RefreshBlacklist();
                    break;
            }
        }

        public void KillProcess(int pid, bool blacklist, int actionVersion = 0)
        {
            var version = actionVersion > 0
                ? actionVersion
                : BeginDestructiveAction(blacklist);

            Task.Run(() => _processesService.Kill(pid))
                .ContinueWith(task =>
                {
                    var (name, result) = task.Result;
                    InvokeOnUi(() =>
                    {
                        Log("KILL", pid.ToString(), name, result);

                        if (blacklist && !string.IsNullOrEmpty(name))
                            AddToBlacklist(name, BlacklistType.Process);

                        var entry = Processes.FirstOrDefault(process => process.Pid == pid);
                        if (entry != null)
                            Processes.Remove(entry);

                        RestoreIdleStateAfterDelay(version);
                    });
                });
        }

        public void StopAndBlacklistService(string name)
        {
            var version = BeginDestructiveAction(true);
            var entry = Services.FirstOrDefault(service => service.ServiceName == name);

            Task.Run(() =>
            {
                var result = _servicesService.StopOnly(name);
                InvokeOnUi(() =>
                {
                    if (entry != null)
                        entry.Status = "Stopped";

                    Log("SVC-BLOCK", "-", name, result);
                    AddToBlacklist(name, BlacklistType.Service);
                    RestoreIdleStateAfterDelay(version);
                });
            });
        }

        public void ToggleService(string name)
        {
            var entry = Services.FirstOrDefault(service => service.ServiceName == name);

            Task.Run(() =>
            {
                var (status, result) = _servicesService.Toggle(name);
                InvokeOnUi(() =>
                {
                    if (entry != null)
                        entry.Status = status;

                    Log("SVC", "-", name, result);
                });
            });
        }

        public void BlockNetworkProcess(string name, int actionVersion = 0)
        {
            var version = actionVersion > 0
                ? actionVersion
                : BeginDestructiveAction(true);

            AddToBlacklist(name, BlacklistType.Process);
            RestoreIdleStateAfterDelay(version);
        }

        public void ToggleStartup(StartupEntry entry)
        {
            _startupService.Toggle(entry);
            Log("STARTUP", "-", entry.Name, entry.IsEnabled ? "enabled" : "disabled");
        }

        public void AddToBlacklist(string name, BlacklistType type)
        {
            _blacklistService.Add(name, type);

            if (_activeTab == ActiveTab.Blacklist)
                RefreshBlacklist();

            Log("BLACKLIST", "-", name, $"added ({type})");
        }

        public void RemoveFromBlacklist(int id)
        {
            var entry = Blacklist.FirstOrDefault(item => item.Id == id);
            _blacklistService.Remove(id);

            if (entry != null)
                Blacklist.Remove(entry);

            Log("BLACKLIST", "-", entry?.Name ?? "?", "removed");
            StatusText = $"{Blacklist.Count} blacklisted";
        }

        public void SetBlacklistActive(int id, bool active)
        {
            _blacklistService.SetActive(id, active);
            var entry = Blacklist.FirstOrDefault(item => item.Id == id);

            if (entry != null)
                entry.IsActive = active;

            Log("BLACKLIST", "-", entry?.Name ?? "?", active ? "enforcement ON" : "paused");
        }

        public void ClearLog()
        {
            KillLog.Clear();
            KillCount = 0;
        }

        private void LoadInitialProcesses()
        {
            _mainTimer.Stop();

            Task.Run(() =>
            {
                var empty = new Dictionary<int, long>();
                var processes = _processesService.GetProcesses("", empty, empty);

                InvokeOnUi(() =>
                {
                    foreach (var process in processes)
                        Processes.Add(process);

                    StatusText = $"{Processes.Count} processes";
                    Tick();
                    CheckVpn();
                    _mainTimer.Start();
                });
            });
        }

        private void ApplySearch()
        {
            _searchTimer.Stop();
            _appliedSearch = _pendingSearch;
            Refresh();

            if (string.IsNullOrEmpty(_appliedSearch))
                AnimeState = AnimeState.Normal;
        }

        private void Tick()
        {
            (_downloadSpeeds, _uploadSpeeds) = _networkMonitor.Tick();
            var (down, up) = _networkMonitor.GetTotals(_downloadSpeeds, _uploadSpeeds);

            TotalDown = FormatBytes(down);
            TotalUp = FormatBytes(up);

            CheckNetworkLimits();
            Refresh();
        }

        private void RefreshProcesses()
        {
            var fresh = _processesService
                .GetProcesses(_appliedSearch, _downloadSpeeds, _uploadSpeeds);
            var byPid = fresh.ToDictionary(process => process.Pid);

            for (var i = Processes.Count - 1; i >= 0; i--)
            {
                if (!byPid.ContainsKey(Processes[i].Pid))
                    Processes.RemoveAt(i);
            }

            RemoveDuplicateProcesses();

            var existing = Processes.ToDictionary(process => process.Pid);
            foreach (var process in fresh)
            {
                if (existing.TryGetValue(process.Pid, out var current))
                {
                    current.CpuPercent = process.CpuPercent;
                    current.MemoryMb = process.MemoryMb;
                    current.DownloadSpeed = process.DownloadSpeed;
                    current.UploadSpeed = process.UploadSpeed;
                    current.IsService = process.IsService;
                    current.CriticalWarning = process.CriticalWarning;
                }
                else
                {
                    Processes.Add(process);
                }
            }

            StatusText = $"{Processes.Count} processes";
        }

        private void RemoveDuplicateProcesses()
        {
            var seen = new HashSet<int>();
            var duplicates = new List<int>();

            for (var i = 0; i < Processes.Count; i++)
            {
                if (!seen.Add(Processes[i].Pid))
                    duplicates.Add(i);
            }

            for (var i = duplicates.Count - 1; i >= 0; i--)
                Processes.RemoveAt(duplicates[i]);
        }

        private void RefreshServices()
        {
            Services.Clear();
            foreach (var service in _servicesService.GetServices(_appliedSearch))
                Services.Add(service);

            StatusText = $"{Services.Count} services";
        }

        private void RefreshNetwork()
        {
            var fresh = _networkMonitor.BuildEntries(
                _appliedSearch,
                _downloadSpeeds,
                _uploadSpeeds);
            var limits = Network.ToDictionary(
                entry => entry.Pid,
                entry => (entry.LimitEnabled, entry.LimitBytes));

            Network.Clear();
            foreach (var entry in fresh)
            {
                if (limits.TryGetValue(entry.Pid, out var limit))
                {
                    entry.LimitEnabled = limit.LimitEnabled;
                    entry.LimitBytes = limit.LimitBytes;
                }

                Network.Add(entry);
            }

            StatusText = $"{Network.Count} connections";
        }

        private void RefreshStartup()
        {
            Startup.Clear();
            foreach (var entry in _startupService.GetStartupEntries(_appliedSearch))
                Startup.Add(entry);

            StatusText = $"{Startup.Count} startup items";
        }

        private void RefreshBlacklist()
        {
            var entries = _blacklistService.GetAll();
            var filter = _appliedSearch?.ToLowerInvariant() ?? "";

            if (!string.IsNullOrEmpty(filter))
                entries = entries
                    .Where(entry => entry.Name.ToLowerInvariant().Contains(filter))
                    .ToList();

            Blacklist.Clear();
            foreach (var entry in entries)
                Blacklist.Add(entry);

            StatusText = $"{Blacklist.Count} blacklisted";
        }

        private void CheckNetworkLimits()
        {
            foreach (var entry in Network)
            {
                if (!entry.LimitEnabled || entry.TotalBytes < entry.LimitBytes)
                    continue;

                Log("NET-LIMIT", entry.Pid.ToString(), entry.Name, $"hit {FormatBytes(entry.LimitBytes)} -> blocking");
                AddToBlacklist(entry.Name, BlacklistType.Process);
                entry.LimitEnabled = false;
            }

            if (!_globalNetLimitEnabled)
                return;

            var globalLimit = (long)(_globalNetLimitMb * 1_048_576);
            foreach (var entry in Network.Where(entry => entry.TotalBytes >= globalLimit))
            {
                Log("GLOBAL-LIMIT", entry.Pid.ToString(), entry.Name, "global limit -> blocking");
                AddToBlacklist(entry.Name, BlacklistType.Process);
            }
        }

        private void CheckVpn()
        {
            var status = _vpnDetector.Check();
            VpnActive = status.IsActive;
            VpnLabel = status.IsActive ? $"VPN ACTIVE - {status.Name}" : "NO VPN DETECTED";
        }

        private void RestoreIdleStateAfterDelay(int version)
        {
            Task.Delay(2500).ContinueWith(_ =>
                InvokeOnUi(() =>
                {
                    if (version == _actionVersion)
                        AnimeState = string.IsNullOrEmpty(_pendingSearch)
                            ? AnimeState.Normal
                            : AnimeState.Searching;
                }));
        }

        private void Log(string type, string pid, string name, string result)
        {
            KillLog.Insert(0, new KillLogEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Tag = $"[{type}:{pid}]",
                Name = name,
                Result = result
            });
            KillCount = KillLog.Count;
            KillLogUpdated?.Invoke();
        }

        private static void InvokeOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B/s";
            if (bytes < 1024) return $"{bytes} B/s";
            if (bytes < 1_048_576) return $"{bytes / 1024.0:F1} KB/s";
            return $"{bytes / 1_048_576.0:F1} MB/s";
        }

        private void Set<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
