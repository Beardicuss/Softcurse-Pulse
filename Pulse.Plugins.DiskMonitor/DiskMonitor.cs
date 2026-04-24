using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pulse.Core;

namespace Pulse.Plugins.DiskMonitor
{
    public class DiskMonitorModule : IModule
    {
        public string Name => "Disk Space Monitor (Plugin)";
        public bool IsEnabled { get; set; } = true;

        private CancellationTokenSource _cts;
        private readonly ActionEngine _actionEngine;

        public DiskMonitorModule(ActionEngine actionEngine)
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
                    await Task.Delay(30000, token);
                    continue;
                }

                try
                {
                    var rootPath = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory);
                    if (string.IsNullOrEmpty(rootPath)) rootPath = "C:\\";

                    var drive = new DriveInfo(rootPath);
                    if (drive.IsReady)
                    {
                        double freeSpaceGB = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                        double totalSpaceGB = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
                        
                        double freePercent = (freeSpaceGB / totalSpaceGB) * 100;

                        if (freePercent < 15)
                        {
                            _actionEngine.ExecuteAction("Low Disk Space", () => 
                            {
                                _actionEngine.NotifyUser("Storage Warning", $"Drive {drive.Name} is running critically low! Only {freeSpaceGB:F1}GB ({freePercent:F1}%) remaining.");
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"DiskMonitor Plugin Error: {ex.Message}");
                }

                await Task.Delay(60000, token);
            }
        }
    }
}
