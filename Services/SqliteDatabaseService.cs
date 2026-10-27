using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using TavrynexoAntiSpy.Models;

namespace TavrynexoAntiSpy.Services
{
    public static class SqliteDatabaseService
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TavrynexoAntiSpy");

        private static readonly string DbPath = Path.Combine(AppDataDir, "TavrynexoAntiSpy.db");
        private static readonly string ConnectionString = $"Data Source={DbPath}";

        static SqliteDatabaseService()
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                {
                    Directory.CreateDirectory(AppDataDir);
                }
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SQLite Init Error] {ex.Message}");
            }
        }

        private static void InitializeDatabase()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string createTablesQuery = @"
                CREATE TABLE IF NOT EXISTS ActivityLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    RuleId TEXT,
                    RuleTitle TEXT,
                    ActionType TEXT,
                    Target TEXT,
                    OldValue TEXT,
                    NewValue TEXT,
                    IsSuccess INTEGER,
                    Message TEXT
                );
            ";

            using var command = new SqliteCommand(createTablesQuery, connection);
            command.ExecuteNonQuery();
        }

        public static void SaveLog(AuditLogEntry entry)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                string insertSql = @"
                    INSERT INTO ActivityLogs (Timestamp, RuleId, RuleTitle, ActionType, Target, OldValue, NewValue, IsSuccess, Message)
                    VALUES (@Timestamp, @RuleId, @RuleTitle, @ActionType, @Target, @OldValue, @NewValue, @IsSuccess, @Message);
                ";

                using var cmd = new SqliteCommand(insertSql, connection);
                cmd.Parameters.AddWithValue("@Timestamp", entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@RuleId", entry.RuleId ?? string.Empty);
                cmd.Parameters.AddWithValue("@RuleTitle", entry.RuleTitle ?? string.Empty);
                cmd.Parameters.AddWithValue("@ActionType", entry.ActionType ?? string.Empty);
                cmd.Parameters.AddWithValue("@Target", entry.Target ?? string.Empty);
                cmd.Parameters.AddWithValue("@OldValue", entry.OldValue ?? string.Empty);
                cmd.Parameters.AddWithValue("@NewValue", entry.NewValue ?? string.Empty);
                cmd.Parameters.AddWithValue("@IsSuccess", entry.IsSuccess ? 1 : 0);
                cmd.Parameters.AddWithValue("@Message", entry.Message ?? string.Empty);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SQLite SaveLog Error] {ex.Message}");
            }
        }

        public static List<AuditLogEntry> LoadRecentLogs(int limit = 150)
        {
            var list = new List<AuditLogEntry>();
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                string query = "SELECT Timestamp, RuleId, RuleTitle, ActionType, Target, OldValue, NewValue, IsSuccess, Message FROM ActivityLogs ORDER BY Id DESC LIMIT @Limit;";
                using var cmd = new SqliteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Limit", limit);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime.TryParse(reader.GetString(0), out var dt);
                    list.Add(new AuditLogEntry
                    {
                        Timestamp = dt,
                        RuleId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        RuleTitle = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        ActionType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Target = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        OldValue = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        NewValue = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        IsSuccess = reader.GetInt32(7) == 1,
                        Message = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SQLite LoadLogs Error] {ex.Message}");
            }
            return list;
        }

        public static string GetDatabaseFilePath() => DbPath;
    }
}
