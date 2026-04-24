using System;
using System.IO;

namespace Pulse.Core
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pulse.log");
        private static readonly object LockObject = new object();

        public static void Log(string message, string level = "INFO")
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            Console.WriteLine(logEntry);

            lock (LockObject)
            {
                try
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        public static void Info(string message) => Log(message, "INFO");
        public static void Warning(string message) 
        {
            Log(message, "WARN");
            DatabaseManager.LogAnomaly("WARN", message);
        }
        public static void Error(string message) 
        {
            Log(message, "ERROR");
            DatabaseManager.LogAnomaly("ERROR", message);
        }
    }
}
