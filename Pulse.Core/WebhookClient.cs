using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pulse.Core
{
    public class WebhookClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public static async Task SendDiscordAlertAsync(string webhookUrl, string title, string message)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;
            
            try
            {
                var payload = new
                {
                    embeds = new[]
                    {
                        new {
                            title = "Pulse System Alert: " + title,
                            description = message,
                            color = 16711680 // Red
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(webhookUrl, content);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send Discord webhook: {ex.Message}");
            }
        }
        
        public static async Task SendTelegramAlertAsync(string botToken, string chatId, string title, string message)
        {
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId)) return;

            try
            {
                string url = $"https://api.telegram.org/bot{botToken}/sendMessage";
                var payload = new
                {
                    chat_id = chatId,
                    text = $"🚨 *Pulse System Alert: {title}*\n\n{message}",
                    parse_mode = "Markdown"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send Telegram webhook: {ex.Message}");
            }
        }
    }
}
