using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Pulse.Core;

namespace Pulse.Plugins.BatteryMonitor
{
    public class BatteryMonitorModule : IModule
    {
        public string Name => "Battery Tracker (Plugin)";
        public bool IsEnabled { get; set; } = true;

        private CancellationTokenSource _cts;
        private readonly ActionEngine _actionEngine;

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

        public BatteryMonitorModule(ActionEngine actionEngine)
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
                    if (GetSystemPowerStatus(out var status))
                    {
                        if (status.ACLineStatus == 0 && status.BatteryFlag != 128)
                        {
                            if (status.BatteryLifePercent > 0 && status.BatteryLifePercent <= 20)
                            {
                                _actionEngine.ExecuteAction("Low Battery", () => 
                                {
                                    _actionEngine.NotifyUser("Battery System Warning", $"Critical: Laptop battery has dropped to {status.BatteryLifePercent}%!");
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"BatteryMonitor Plugin Error: {ex.Message}");
                }

                await Task.Delay(180000, token); 
            }
        }
    }
}
