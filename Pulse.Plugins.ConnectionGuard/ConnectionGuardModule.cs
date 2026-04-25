using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Pulse.Core;

namespace Pulse.Plugins.ConnectionGuard
{
    public class ConnectionGuardModule : IModule
    {
        public string Name => "Connection Guard (Plugin)";
        public bool IsEnabled { get; set; } = true;

        private CancellationTokenSource _cts;
        private readonly ActionEngine _actionEngine;
        private bool _wasConnected = true;

        public ConnectionGuardModule(ActionEngine actionEngine)
        {
            _actionEngine = actionEngine;
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => MonitorLoop(_cts.Token));
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!IsEnabled)
                {
                    await Task.Delay(10000, token);
                    continue;
                }

                try
                {
                    bool isConnected = false;
                    using (Ping ping = new Ping())
                    {
                        var reply = await ping.SendPingAsync("1.1.1.1", 2000);
                        if (reply.Status == IPStatus.Success)
                        {
                            isConnected = true;
                        }
                    }

                    if (isConnected && !_wasConnected)
                    {
                        _actionEngine.ExecuteAction("Connection Restored", () =>
                        {
                            _actionEngine.NotifyUser("Network Security", "External Internet connection has been restored successfully.");
                        });
                        _wasConnected = true;
                    }
                    else if (!isConnected && _wasConnected)
                    {
                        _actionEngine.ExecuteAction("Connection Dropped", () =>
                        {
                            _actionEngine.NotifyUser("Network Security", "CRITICAL: External Internet connection dropped entirely! Device isolated.");
                        });
                        _wasConnected = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"ConnectionGuard Plugin Error: {ex.Message}");
                }

                await Task.Delay(5000, token);
            }
        }
    }
}
