using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pulse.Core;

namespace Pulse.App
{
    static class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DatabaseManager.Initialize();
            Logger.Info("Pulse is starting...");

            var actionEngine = new ActionEngine();
            var configManager = new ConfigManager();

            actionEngine.OnAlertRequested += (title, message) => 
            {
                _ = WebhookClient.SendDiscordAlertAsync(configManager.CurrentConfig.DiscordWebhookUrl, title, message);
                _ = WebhookClient.SendTelegramAlertAsync(configManager.CurrentConfig.TelegramBotToken, configManager.CurrentConfig.TelegramChatId, title, message);
            };

            var modules = new List<IModule>
            {
                new NetworkMonitor(actionEngine, configManager),
                new ProcessMonitor(actionEngine, configManager),
                actionEngine
            };

            var pluginsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            var pluginModules = PluginLoader.LoadPlugins(pluginsDir, actionEngine, configManager);
            modules.AddRange(pluginModules);

            foreach (var module in modules)
            {
                if (module.IsEnabled)
                {
                    Logger.Info($"Starting module: {module.Name}");
                    await module.StartAsync();
                }
            }

            // Run the tray application
            var trayApp = new TrayApplication(modules, configManager);
            
            // We use Task.Run to keep the UI thread separate if needed, 
            // but Application.Run blocks, so we handle shutdown via the tray app.
            Application.Run(trayApp);

            Logger.Info("Pulse is shutting down...");
            foreach (var module in modules)
            {
                await module.StopAsync();
            }
        }
    }
}
