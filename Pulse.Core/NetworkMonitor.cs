using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public class NetworkMonitor : IModule
    {
        public string Name => "Network Monitor";
        public bool IsEnabled { get; set; } = true;

        private CancellationTokenSource _cts;
        private const string TargetHost = "8.8.8.8"; // Google DNS
        
        private readonly ActionEngine _actionEngine;
        private readonly ConfigManager _configManager;
        private readonly AnomalyDetector _latencyDetector = new AnomalyDetector(20, 2.5);
        private int _consecutiveDrops = 0;
        private DateTime _lastWifiAlert = DateTime.MinValue;
        private DateTime _lastOfflineAlert = DateTime.MinValue;

        public NetworkMonitor(ActionEngine actionEngine, ConfigManager configManager)
        {
            _actionEngine = actionEngine;
            _configManager = configManager;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();
            _ = Task.Run(() => MonitorLoop(_cts.Token));
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts?.Cancel();
            await Task.CompletedTask;
        }

        private void CheckPhysicalAdapters()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            bool hasActiveConnection = false;
            
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up && 
                   (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    hasActiveConnection = true;
                    
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        CheckWifiSignal(ni.Name);
                    }
                }
            }

            if (!hasActiveConnection)
            {
                if ((DateTime.Now - _lastOfflineAlert).TotalMinutes > 1)
                {
                    _lastOfflineAlert = DateTime.Now;
                    _actionEngine.ExecuteAction("No Link", () => 
                    {
                        _actionEngine.NotifyUser("Network Alert", "No active Ethernet or Wi-Fi adapters detected! Disconnected?");
                    });
                }
            }
        }

        private void CheckWifiSignal(string interfaceName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show interfaces",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("Signal"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 1)
                        {
                            var signalStr = parts[1].Trim().Replace("%", "");
                            if (int.TryParse(signalStr, out int signal))
                            {
                                _actionEngine.RecordMetric("WiFi Signal %", signal);
                                
                                if (signal < 45 && (DateTime.Now - _lastWifiAlert).TotalMinutes > 5)
                                {
                                    _lastWifiAlert = DateTime.Now;
                                    _actionEngine.ExecuteAction("Weak WiFi", () => 
                                    {
                                        _actionEngine.NotifyUser("Wi-Fi Alert", $"Weak Wi-Fi connection detected ({signal}%). You may experience latency.");
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Wifi check error: {ex.Message}");
            }
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            using var ping = new Ping();
            while (!token.IsCancellationRequested)
            {
                int intervalMs = _configManager.CurrentConfig.NetworkPollingIntervalMs;
                if (!IsEnabled)
                {
                    await Task.Delay(intervalMs, token);
                    continue;
                }

                try
                {
                    CheckPhysicalAdapters();

                    var reply = await ping.SendPingAsync(TargetHost, 2000);
                    if (reply.Status == IPStatus.Success)
                    {
                        var latency = reply.RoundtripTime;
                        _actionEngine.RecordMetric("Latency", latency);

                        _consecutiveDrops = 0;
                        
                        if (_latencyDetector.IsAnomaly(latency) && latency > 80)
                        {
                            _actionEngine.ExecuteAction("Latency Anomaly", () => 
                            {
                                _actionEngine.NotifyUser("Network Warning", $"Statistical latency spike detected: {latency}ms!");
                            });
                        }
                    }
                    else
                    {
                        Logger.Warning($"Network Issue: {reply.Status}");
                        _consecutiveDrops++;
                        
                        if (_consecutiveDrops == 3)
                        {
                            _actionEngine.ExecuteAction("Connection Lost", () => 
                            {
                                _actionEngine.NotifyUser("Network Alert", "Connection to the internet is down (3 drops). Attempting auto-resolution...");
                                _actionEngine.ResetNetworkAdapter();
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Network Monitor Error: {ex.Message}");
                    _consecutiveDrops++;
                    if (_consecutiveDrops == 3)
                    {
                        _actionEngine.NotifyUser("Network Alert", "Failed to ping target host.");
                    }
                }

                await Task.Delay(intervalMs, token);
            }
        }
    }
}
