using System;
using System.IO;
using System.Threading.Tasks;
using Pulse.Core;

namespace Pulse.Plugins.HostFileGuard
{
    public class HostFileGuardModule : IModule, IDisposable
    {
        public string Name => "Host File Defender (Plugin)";
        public bool IsEnabled { get; set; } = true;
        
        private readonly ActionEngine _actionEngine;
        private FileSystemWatcher _watcher;

        public HostFileGuardModule(ActionEngine actionEngine)
        {
            _actionEngine = actionEngine;
        }

        public Task StartAsync()
        {
            try
            {
                var dir = @"C:\Windows\System32\drivers\etc";
                if (Directory.Exists(dir))
                {
                    _watcher = new FileSystemWatcher(dir, "hosts");
                    _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
                    _watcher.Changed += OnFileChanged;
                    _watcher.Created += OnFileChanged;
                    _watcher.Deleted += OnFileChanged;
                    _watcher.Renamed += OnFileChanged;
                    _watcher.EnableRaisingEvents = IsEnabled;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HostFileGuard Error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!IsEnabled) return;

            _actionEngine.ExecuteAction("Host File Tampering", () => 
            {
                _actionEngine.NotifyUser("Security Alert", "CRITICAL: The Windows HOSTS file has been accessed or modified by an unknown process!");
            });
        }

        public Task StopAsync()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
            return Task.CompletedTask;
        }

        public void Dispose() => StopAsync().Wait();
    }
}
