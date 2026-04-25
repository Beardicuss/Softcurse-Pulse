using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public class ProcessMonitor : IModule
    {
        public string Name => "Process Monitor";
        public bool IsEnabled { get; set; } = true;

        private CancellationTokenSource _cts;

        private readonly ActionEngine   _actionEngine;
        private readonly ConfigManager  _configManager;

        private Dictionary<int, TimeSpan> _previousCpuTimes = new Dictionary<int, TimeSpan>();

        // FIX #8: Track which PIDs have already fired a "suspicious process" alert.
        // Without this, the alert fires on EVERY poll cycle for every running suspicious
        // process. The cooldown in ActionEngine throttled the visible effect to 1 per
        // 30 minutes, but it wasted the slot and meant NEW suspicious processes were
        // silenced while an old one was still running.
        private readonly HashSet<int> _alertedSuspiciousPids = new HashSet<int>();

        // FIX #9 (memory): Track which PIDs have already fired a high-memory alert
        // so we don't spam on every cycle.
        private readonly HashSet<int> _alertedHighMemoryPids = new HashSet<int>();

        public ProcessMonitor(ActionEngine actionEngine, ConfigManager configManager)
        {
            _actionEngine  = actionEngine;
            _configManager = configManager;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => MonitorLoop(_cts.Token));
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts?.Cancel();
            await Task.CompletedTask;
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                int    intervalMs    = _configManager.CurrentConfig.ProcessPollingIntervalMs;
                double cpuThreshold  = _configManager.CurrentConfig.CpuThresholdPercent;
                var    suspiciousList = _configManager.CurrentConfig.SuspiciousProcesses;

                if (!IsEnabled)
                {
                    await Task.Delay(intervalMs, token);
                    continue;
                }

                try
                {
                    var processes       = Process.GetProcesses();
                    var currentCpuTimes = new Dictionary<int, TimeSpan>();
                    var currentPids     = new HashSet<int>(processes.Select(p => p.Id));
                    int processorCount  = Environment.ProcessorCount;

                    // Prune stale PID entries so recycled PIDs are treated as new.
                    _alertedSuspiciousPids.IntersectWith(currentPids);
                    _alertedHighMemoryPids.IntersectWith(currentPids);

                    foreach (var p in processes)
                    {
                        // ── 1. Blacklist check ────────────────────────────────────
                        // FIX #8: Only alert once per process instance (by PID).
                        if (!_alertedSuspiciousPids.Contains(p.Id))
                        {
                            // A) Explicit Blacklist Name Match
                            if (suspiciousList.Contains(p.ProcessName.ToLowerInvariant()))
                            {
                                _alertedSuspiciousPids.Add(p.Id);
                                string pName = p.ProcessName;
                                _actionEngine.ExecuteAction("Suspicious Process", () =>
                                    _actionEngine.NotifyUser("Security Alert",
                                        $"Suspicious process mapped to Explicit Blacklist: {pName} (PID {p.Id})"));
                            }
                            else
                            {
                                // B) Dynamic Behavioral Heuristic (Execution from Temp/Hidden paths)
                                try
                                {
                                    var path = p.MainModule?.FileName;
                                    if (path != null && (path.Contains(@"\AppData\Local\Temp\") || path.Contains(@"\Windows\Temp\")))
                                    {
                                        _alertedSuspiciousPids.Add(p.Id);
                                        string pName = p.ProcessName;
                                        _actionEngine.ExecuteAction("Heuristic Suspicious", () =>
                                            _actionEngine.NotifyUser("Advanced Security Alert",
                                                $"WARNING: Unapproved dynamic executable detected spooling from Temp Path: {pName}.exe (PID {p.Id})"));
                                    }
                                }
                                catch { /* System processes block MainModule access natively */ }
                            }
                        }

                        // ── 2. CPU tracking ──────────────────────────────────────
                        try
                        {
                            currentCpuTimes[p.Id] = p.TotalProcessorTime;

                            if (_previousCpuTimes.TryGetValue(p.Id, out var previousTime))
                            {
                                var cpuUsedMs         = (currentCpuTimes[p.Id] - previousTime).TotalMilliseconds;
                                var totalMsPassed      = intervalMs * processorCount;
                                var cpuUsagePercentage = (cpuUsedMs / totalMsPassed) * 100;

                                if (cpuUsagePercentage > cpuThreshold)
                                {
                                    string pName   = p.ProcessName;
                                    double cpuPct  = cpuUsagePercentage;
                                    _actionEngine.ExecuteAction("High CPU", () =>
                                        _actionEngine.NotifyUser("Performance Alert",
                                            $"{pName} is using {cpuPct:F1}% CPU"));
                                }
                            }
                        }
                        catch
                        {
                            // Access denied to system processes — safely ignore.
                        }
                    }

                    _previousCpuTimes = currentCpuTimes;

                    // ── 3. Memory tracking ────────────────────────────────────────
                    // FIX #9: Was dead code (empty loop with a comment). Now actually alerts.
                    var highMemoryProcesses = processes
                        .Where(p => p.WorkingSet64 > 500 * 1024 * 1024) // > 500 MB
                        .OrderByDescending(p => p.WorkingSet64)
                        .Take(3);

                    foreach (var p in highMemoryProcesses)
                    {
                        // Alert once per process instance.
                        if (_alertedHighMemoryPids.Contains(p.Id)) continue;
                        _alertedHighMemoryPids.Add(p.Id);

                        string pName  = p.ProcessName;
                        long   memMb  = p.WorkingSet64 / (1024 * 1024);
                        _actionEngine.ExecuteAction("High Memory", () =>
                            _actionEngine.NotifyUser("Performance Alert",
                                $"{pName} is consuming {memMb} MB of RAM"));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Process Monitor Error: {ex.Message}");
                }

                await Task.Delay(intervalMs, token);
            }
        }
    }
}
