using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Pulse.Core;

namespace Pulse.Plugins.RegistryWatcher
{
    public class RegistryWatcherModule : IModule
    {
        public string Name => "Registry Autorun Watcher (Plugin)";
        public bool IsEnabled { get; set; } = true;

        private CancellationTokenSource _cts;
        private readonly ActionEngine _actionEngine;
        private HashSet<string> _knownEntries = new HashSet<string>();

        public RegistryWatcherModule(ActionEngine actionEngine)
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
            SnapshotRegistry();

            while (!token.IsCancellationRequested)
            {
                if (!IsEnabled)
                {
                    await Task.Delay(10000, token);
                    continue;
                }

                try
                {
                    CheckRegistry();
                }
                catch (Exception ex)
                {
                    Logger.Error($"RegistryWatcher Plugin Error: {ex.Message}");
                }

                await Task.Delay(10000, token);
            }
        }

        private void SnapshotRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (key != null)
                {
                    foreach (var valName in key.GetValueNames())
                    {
                        _knownEntries.Add(valName);
                    }
                }
            }
            catch { }
        }

        private void CheckRegistry()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (key != null)
            {
                var currentNames = key.GetValueNames();
                foreach (var name in currentNames)
                {
                    if (!_knownEntries.Contains(name))
                    {
                        string path = key.GetValue(name)?.ToString() ?? "Unknown Path";
                        _actionEngine.ExecuteAction("Rogue Autorun", () =>
                        {
                            _actionEngine.NotifyUser("Security Warning", $"Suspicious process '{name}' attached itself to Registry Startup! Path: {path}");
                        });
                        
                        _knownEntries.Add(name);
                    }
                }
            }
        }
    }
}
