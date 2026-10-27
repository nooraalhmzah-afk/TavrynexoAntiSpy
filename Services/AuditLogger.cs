using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using TavrynexoAntiSpy.Models;

namespace TavrynexoAntiSpy.Services
{
    public static class AuditLogger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TavrynexoAntiSpy", "Logs");
        private static readonly string LogFile = Path.Combine(LogDir, $"Audit_{DateTime.Now:yyyyMMdd}.log");

        public static ObservableCollection<AuditLogEntry> LogEntries { get; } = new();

        static AuditLogger()
        {
            try
            {
                if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
                var dbLogs = SqliteDatabaseService.LoadRecentLogs(100);
                foreach (var log in dbLogs) LogEntries.Add(log);
            }
            catch { }
        }

        public static void Log(string ruleId, string ruleTitle, string actionType, string target, string oldValue, string newValue, bool isSuccess, string message)
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                RuleId = ruleId,
                RuleTitle = ruleTitle,
                ActionType = actionType,
                Target = target,
                OldValue = oldValue,
                NewValue = newValue,
                IsSuccess = isSuccess,
                Message = message
            };

            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke(() => LogEntries.Insert(0, entry));
            else
                LogEntries.Insert(0, entry);

            SqliteDatabaseService.SaveLog(entry);
            try { File.AppendAllText(LogFile, entry.ToString() + Environment.NewLine); } catch { }
        }
    }
}
