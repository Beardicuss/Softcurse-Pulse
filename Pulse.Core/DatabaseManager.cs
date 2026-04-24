using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Pulse.Core
{
    public static class DatabaseManager
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pulse.db");
        private static readonly object _lockObj = new object();
        
        public static void Initialize()
        {
            lock (_lockObj)
            {
                using var connection = new SqliteConnection($"Data Source={DbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Logs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Level TEXT,
                        Message TEXT
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        public static void LogAnomaly(string level, string message)
        {
            lock (_lockObj)
            {
                try
                {
                    using var connection = new SqliteConnection($"Data Source={DbPath}");
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Logs (Timestamp, Level, Message)
                        VALUES ($timestamp, $level, $message);
                    ";
                    command.Parameters.AddWithValue("$timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("$level", level);
                    command.Parameters.AddWithValue("$message", message);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                }
            }
        }

        public static System.Collections.Generic.List<string> GetRecentAnomalies(int count = 50)
        {
            var results = new System.Collections.Generic.List<string>();
            lock (_lockObj)
            {
                try
                {
                    using var connection = new SqliteConnection($"Data Source={DbPath}");
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Timestamp, Level, Message FROM Logs ORDER BY Id DESC LIMIT $count;";
                    command.Parameters.AddWithValue("$count", count);

                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        string ts = reader.GetString(0);
                        string lvl = reader.GetString(1);
                        string msg = reader.GetString(2);
                        results.Add($"[{ts}] [{lvl}] {msg}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Database Error: {ex.Message}");
                }
            }
            return results;
        }
    }
}
