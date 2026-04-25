using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pulse.Core;

namespace Pulse.Plugins.EventLogMonitor
{
    public class EventLogMonitorModule : IModule
    {
        public string Name => "Windows Event Log Monitor (Plugin)";
        public bool IsEnabled { get; set; } = true;
        
        private readonly ActionEngine _actionEngine;
        private EventLog _eventLog;

        public EventLogMonitorModule(ActionEngine actionEngine)
        {
            _actionEngine = actionEngine;
        }

        public Task StartAsync()
        {
            try
            {
                _eventLog = new EventLog("Application");
                _eventLog.EntryWritten += OnEntryWritten;
                _eventLog.EnableRaisingEvents = IsEnabled;
            }
            catch (Exception ex)
            {
                Logger.Error($"EventLogMonitor Error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private void OnEntryWritten(object sender, EntryWrittenEventArgs e)
        {
            if (!IsEnabled) return;
            
            if (e.Entry.EntryType == EventLogEntryType.Error || e.Entry.EntryType == EventLogEntryType.FailureAudit)
            {
                // ID 1000 corresponds strictly to Background Application Crashes (AppCrash).
                if (e.Entry.InstanceId == 1000) 
                {
                    string source = e.Entry.Source;
                    _actionEngine.ExecuteAction("App Crash Detected", () => 
                    {
                        _actionEngine.NotifyUser("System Warning", $"Background Application Crash Intercepted natively! Source origin: {source}");
                    });
                }
            }
        }

        public Task StopAsync()
        {
            if (_eventLog != null)
            {
                _eventLog.EnableRaisingEvents = false;
                _eventLog.Dispose();
            }
            return Task.CompletedTask;
        }
    }
}
