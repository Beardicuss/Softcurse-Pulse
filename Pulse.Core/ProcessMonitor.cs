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
        
        private readonly ActionEngine _actionEngine;
        private readonly ConfigManager _configManager;
        private Dictionary<int, TimeSpan> _previousCpuTimes = new Dictionary<int, TimeSpan>();

        public ProcessMonitor(ActionEngine actionEngine, ConfigManager configManager)
        {
            _actionEngine = actionEngine;
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
                int intervalMs = _configManager.CurrentConfig.ProcessPollingIntervalMs;
                double cpuThreshold = _configManager.CurrentConfig.CpuThresholdPercent;
                var suspiciousProcesses = _configManager.CurrentConfig.SuspiciousProcesses;

                if (!IsEnabled)
                {
                    await Task.Delay(intervalMs, token);
                    continue;
                }

                try
                {
                    var processes = Process.GetProcesses();
                    var currentCpuTimes = new Dictionary<int, TimeSpan>();
                    int processorCount = Environment.ProcessorCount;

                    foreach (var p in processes)
                    {
                        // 1. Blacklist check
                        if (suspiciousProcesses.Contains(p.ProcessName.ToLowerInvariant()))
                        {
                            _actionEngine.ExecuteAction("Suspicious Process", () => 
                            {
                                _actionEngine.NotifyUser("Security Alert", $"Suspicious process detected: {p.ProcessName}");
                            });
                        }

                        // 2. CPU tracking
                        try
                        {
                            currentCpuTimes[p.Id] = p.TotalProcessorTime;
                            
                            if (_previousCpuTimes.TryGetValue(p.Id, out var previousTime))
                            {
                                var cpuUsedMs = (currentCpuTimes[p.Id] - previousTime).TotalMilliseconds;
                                var totalMsPassed = intervalMs * processorCount;
                                var cpuUsagePercentage = (cpuUsedMs / totalMsPassed) * 100;

                                if (cpuUsagePercentage > cpuThreshold)
                                {
                                    _actionEngine.ExecuteAction("High CPU", () => 
                                    {
                                        _actionEngine.NotifyUser("Performance Alert", $"{p.ProcessName} is using {cpuUsagePercentage:F1}% CPU");
                                    });
                                }
                            }
                        }
                        catch 
                        {
                            // Access denied to system processes, safely ignore.
                        }
                    }

                    _previousCpuTimes = currentCpuTimes;

                    // 3. Memory tracking
                    var highMemoryProcesses = processes
                        .Where(p => p.WorkingSet64 > 500 * 1024 * 1024) 
                        .OrderByDescending(p => p.WorkingSet64) 
                        .Take(3);

                    foreach (var p in highMemoryProcesses)
                    {
                        // Just maintaining standard memory usage print for now
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
