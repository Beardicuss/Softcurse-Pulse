using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public class ActionEngine : IModule
    {
        public string Name => "Action Engine";
        public bool IsEnabled { get; set; } = true;

        public event Action<string, string> OnAlertRequested;

        public async Task StartAsync()
        {
            Logger.Info("Action Engine initialized.");
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            await Task.CompletedTask;
        }

        public void ExecuteAction(string trigger, Action action)
        {
            if (!IsEnabled) return;
            
            Logger.Info($"Triggered action for: {trigger}");
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"Action execution failed: {ex.Message}");
            }
        }

        private readonly System.Collections.Generic.Dictionary<string, DateTime> _alertCooldowns = new System.Collections.Generic.Dictionary<string, DateTime>();

        public void NotifyUser(string title, string message)
        {
            if (!IsEnabled) return;

            // Throttle duplicate alerts (e.g. Low Disk Space) to once every 4 hours!
            if (_alertCooldowns.TryGetValue(title, out var lastAlert))
            {
                if (DateTime.Now - lastAlert < TimeSpan.FromHours(4))
                    return; 
            }
            _alertCooldowns[title] = DateTime.Now;

            Logger.Info($"NOTIFICATION: {title} - {message}");
            DatabaseManager.LogAnomaly($"ALERT: {title}", message);
            OnAlertRequested?.Invoke(title, message);
        }

        public event Action<string, long> OnMetricRecorded;
        public void RecordMetric(string name, long value)
        {
            if (IsEnabled) OnMetricRecorded?.Invoke(name, value);
        }

        public void QuarantineProcess(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0) return;
                
                string appPath = processes[0].MainModule?.FileName;
                if (string.IsNullOrEmpty(appPath)) return;

                var outInfo = new ProcessStartInfo("netsh", $"advfirewall firewall add rule name=\"Pulse-Block-{processName}\" dir=out action=block program=\"{appPath}\"") 
                { CreateNoWindow = true, UseShellExecute = true, Verb = "runas" };
                Process.Start(outInfo);

                var inInfo = new ProcessStartInfo("netsh", $"advfirewall firewall add rule name=\"Pulse-Block-{processName}\" dir=in action=block program=\"{appPath}\"") 
                { CreateNoWindow = true, UseShellExecute = true, Verb = "runas" };
                Process.Start(inInfo);

                NotifyUser("System Action", $"Successfully quarantined '{processName}'. All inbound and outbound traffic has been permanently blocked by Windows Firewall.");
            }
            catch (Exception ex)
            {
                NotifyUser("Action Failed", $"Could not quarantine {processName}: You must grant UAC Admin permissions for Firewall injection.");
            }
        }

        public void KillProcess(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0) return;
                
                foreach (var p in processes)
                {
                    p.Kill();
                }
                NotifyUser("System Action", $"Successfully terminated process: {processName}");
            }
            catch (Exception ex)
            {
                NotifyUser("Action Failed", $"Could not kill {processName}: {ex.Message}");
            }
        }

        public void ResetNetworkAdapter()
        {
            try
            {
                Process.Start(new ProcessStartInfo("cmd", "/c ipconfig /flushdns") { CreateNoWindow = true, UseShellExecute = false });
                NotifyUser("Network Action", "DNS Cache flushed to resolve drop.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Network reset failed: {ex.Message}");
            }
        }
    }
}
