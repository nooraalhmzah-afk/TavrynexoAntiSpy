using System.Collections.Generic;
using TavrynexoAntiSpy.Models;

namespace TavrynexoAntiSpy.Services
{
    public static class PrivacyRulesRepository
    {
        public static List<PrivacyRule> GetAllRules()
        {
            return new List<PrivacyRule>
            {
                new PrivacyRule
                {
                    Id = "TEL_01",
                    Title = "Disable Windows Diagnostic Data & Telemetry (AllowTelemetry)",
                    Description = "Prevents Windows from sending telemetry and usage statistics to Microsoft servers.",
                    Category = RuleCategory.Telemetry,
                    Recommendation = RecommendationLevel.Recommended,
                    TargetType = RuleTargetType.RegistryDWord,
                    Hive = RegistryHiveType.LocalMachine,
                    KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    ValueName = "AllowTelemetry",
                    DesiredValue = 0,
                    DefaultWindowsValue = 3
                },
                new PrivacyRule
                {
                    Id = "SVC_01",
                    Title = "Disable Connected User Experiences & Telemetry Service (DiagTrack)",
                    Description = "Permanently disables the primary Windows telemetry daemon (DiagTrack).",
                    Category = RuleCategory.Services,
                    Recommendation = RecommendationLevel.Recommended,
                    TargetType = RuleTargetType.WindowsService,
                    ServiceName = "DiagTrack",
                    DesiredValue = 4,
                    DefaultWindowsValue = 2
                },
                new PrivacyRule
                {
                    Id = "ADV_01",
                    Title = "Disable User Advertising ID for Apps",
                    Description = "Prevents apps from accessing your unique advertising identifier for targeted profiling.",
                    Category = RuleCategory.AdvertisingAndTracking,
                    Recommendation = RecommendationLevel.Recommended,
                    TargetType = RuleTargetType.RegistryDWord,
                    Hive = RegistryHiveType.CurrentUser,
                    KeyPath = @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                    ValueName = "Enabled",
                    DesiredValue = 0,
                    DefaultWindowsValue = 1
                },
                new PrivacyRule
                {
                    Id = "COR_01",
                    Title = "Disable Cortana Voice Assistant",
                    Description = "Disables Cortana integration and background microphone listening.",
                    Category = RuleCategory.CortanaAndSearch,
                    Recommendation = RecommendationLevel.Recommended,
                    TargetType = RuleTargetType.RegistryDWord,
                    Hive = RegistryHiveType.LocalMachine,
                    KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    ValueName = "AllowCortana",
                    DesiredValue = 0,
                    DefaultWindowsValue = 1
                },
                new PrivacyRule
                {
                    Id = "UPD_01",
                    Title = "Disable Windows Update P2P Upload Bandwidth Sharing",
                    Description = "Stops your PC from acting as an update distribution peer uploading across the internet.",
                    Category = RuleCategory.WindowsUpdate,
                    Recommendation = RecommendationLevel.Recommended,
                    TargetType = RuleTargetType.RegistryDWord,
                    Hive = RegistryHiveType.LocalMachine,
                    KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
                    ValueName = "DODownloadMode",
                    DesiredValue = 0,
                    DefaultWindowsValue = 3
                }
            };
        }
    }
}
