using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TavrynexoAntiSpy.Models;

namespace TavrynexoAntiSpy.Services
{
    public class SystemSettingBackup
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleTitle { get; set; } = string.Empty;
        public RuleTargetType TargetType { get; set; }
        public RegistryHiveType Hive { get; set; }
        public string KeyPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public string? OriginalValueString { get; set; }
        public string? ServiceName { get; set; }
        public string? TaskPath { get; set; }
    }

    public class BackupPackage
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string WindowsVersion { get; set; } = Environment.OSVersion.ToString();
        public List<SystemSettingBackup> Items { get; set; } = new();
    }

    public static class BackupService
    {
        private static readonly string BackupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "TavrynexoAntiSpy", "Backups");

        static BackupService()
        {
            try { if (!Directory.Exists(BackupDir)) Directory.CreateDirectory(BackupDir); } catch { }
        }

        public static string CreateBackup(IEnumerable<PrivacyRule> rules)
        {
            var package = new BackupPackage { CreatedAt = DateTime.Now };
            foreach (var rule in rules)
            {
                package.Items.Add(new SystemSettingBackup
                {
                    RuleId = rule.Id,
                    RuleTitle = rule.Title,
                    TargetType = rule.TargetType,
                    Hive = rule.Hive,
                    KeyPath = rule.KeyPath,
                    ValueName = rule.ValueName,
                    ServiceName = rule.ServiceName,
                    TaskPath = rule.TaskPath,
                    OriginalValueString = RegistryManager.ReadValue(rule.Hive, rule.KeyPath, rule.ValueName)?.ToString()
                });
            }

            string filePath = Path.Combine(BackupDir, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true }));
            return filePath;
        }

        public static (bool Success, string Message, int RestoredCount) RestoreFromBackup(string backupFilePath, IEnumerable<PrivacyRule> rules)
        {
            try
            {
                if (!File.Exists(backupFilePath)) return (false, "File not found", 0);
                var package = JsonSerializer.Deserialize<BackupPackage>(File.ReadAllText(backupFilePath));
                if (package == null) return (false, "Corrupt backup", 0);

                int count = 0;
                foreach (var item in package.Items)
                {
                    if (item.TargetType == RuleTargetType.WindowsService && !string.IsNullOrEmpty(item.ServiceName))
                        ServiceManager.EnableAndStartService(item.ServiceName, item.OriginalValueString ?? "auto");
                    else if (item.TargetType == RuleTargetType.ScheduledTask && !string.IsNullOrEmpty(item.TaskPath))
                        TaskSchedulerManager.EnableTask(item.TaskPath);
                    else
                        RegistryManager.WriteAndVerify(item.Hive, item.KeyPath, item.ValueName, item.OriginalValueString ?? "0");
                    count++;
                }
                return (true, $"Restored {count} settings.", count);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0);
            }
        }

        public static string[] GetAvailableBackups() => Directory.Exists(BackupDir) ? Directory.GetFiles(BackupDir, "*.json") : Array.Empty<string>();
    }
}
