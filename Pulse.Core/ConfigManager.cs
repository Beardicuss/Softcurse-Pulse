using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Pulse.Core
{
    public class PulseConfig
    {
        public int NetworkPollingIntervalMs { get; set; } = 5000;
        public int ProcessPollingIntervalMs { get; set; } = 10000;
        public double CpuThresholdPercent { get; set; } = 80.0;
        public List<string> SuspiciousProcesses { get; set; } = new List<string> { "notepad", "miner", "malware" };
        public string DiscordWebhookUrl { get; set; } = "";
        public string TelegramBotToken { get; set; } = "";
        public string TelegramChatId { get; set; } = "";
    }

    public class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        public PulseConfig CurrentConfig { get; private set; }
        
        public event Action OnConfigUpdated;

        public ConfigManager()
        {
            LoadConfig();
        }

        public void LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    CurrentConfig = JsonSerializer.Deserialize<PulseConfig>(json) ?? new PulseConfig();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load config: {ex.Message}");
                    CurrentConfig = new PulseConfig();
                }
            }
            else
            {
                CurrentConfig = new PulseConfig();
                SaveConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(CurrentConfig, options);
                File.WriteAllText(ConfigPath, json);
                OnConfigUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save config: {ex.Message}");
            }
        }
    }
}
