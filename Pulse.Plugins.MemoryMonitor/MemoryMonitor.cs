using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pulse.Core;

namespace Pulse.Plugins.MemoryMonitor
{
    public class MemoryMonitorModule : IModule
    {
        public string Name => "Memory Leak Monitor (Plugin)";
        public bool IsEnabled { get; set; } = true;

        private CancellationTokenSource _cts;
        private readonly ActionEngine _actionEngine;
        
        private readonly Dictionary<string, List<double>> _memoryHistory = new Dictionary<string, List<double>>();

        public MemoryMonitorModule(ActionEngine actionEngine)
        {
            _actionEngine = actionEngine;
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
                if (!IsEnabled)
                {
                    await Task.Delay(60000, token);
                    continue;
                }

                try
                {
                    var processes = Process.GetProcesses();
                    var activeNames = new HashSet<string>();

                    foreach (var p in processes)
                    {
                        try
                        {
                            double memMB = p.WorkingSet64 / 1024.0 / 1024.0;
                            if (memMB < 50) continue; 
                            
                            string name = p.ProcessName;
                            activeNames.Add(name);

                            if (!_memoryHistory.ContainsKey(name))
                                _memoryHistory[name] = new List<double>();

                            _memoryHistory[name].Add(memMB);
                            
                            if (_memoryHistory[name].Count > 10)
                                _memoryHistory[name].RemoveAt(0);

                            var history = _memoryHistory[name];
                            if (history.Count == 10)
                            {
                                bool isLeaking = true;
                                double startMem = history[0];
                                double endMem = history[9];

                                for (int i = 1; i < history.Count; i++)
                                {
                                    if (history[i] < history[i - 1])
                                    {
                                        isLeaking = false;
                                        break;
                                    }
                                }

                                if (isLeaking && (endMem - startMem > 150))
                                {
                                    // Linear leak climbing +150MB over ~10 minutes
                                    _actionEngine.ExecuteAction("Memory Leak", () => 
                                    {
                                        _actionEngine.NotifyUser("Memory Alert", $"Process '{name}' has consistently increased RAM usage! (+{(endMem - startMem):F0}MB unbroken linear gain)");
                                    });
                                    _memoryHistory[name].Clear(); 
                                }
                            }
                        }
                        catch { }
                    }

                    var keysToRemove = _memoryHistory.Keys.Where(k => !activeNames.Contains(k)).ToList();
                    foreach (var k in keysToRemove) _memoryHistory.Remove(k);
                }
                catch (Exception ex)
                {
                    Logger.Error($"MemoryMonitor Error: {ex.Message}");
                }

                await Task.Delay(60000, token);
            }
        }
    }
}
