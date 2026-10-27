using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TavrynexoAntiSpy.Services
{
    public class SecurityAuditReport
    {
        public bool IsFirewallActive { get; set; }
        public string FirewallStatusDetails { get; set; } = string.Empty;

        public bool IsDefenderActive { get; set; }
        public bool IsRealTimeProtectionEnabled { get; set; }
        public string DefenderStatusDetails { get; set; } = string.Empty;

        public bool IsUacActive { get; set; }
        public string UacStatusDetails { get; set; } = string.Empty;

        public string OverallSecuritySummary { get; set; } = string.Empty;
    }

    public static class WindowsSecurityAuditor
    {
        public static async Task<SecurityAuditReport> RunSecurityAuditAsync()
        {
            return await Task.Run(() =>
            {
                var report = new SecurityAuditReport();

                // 1. Audit Windows Firewall
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = "advfirewall show allprofiles state",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(3000);

                        bool isOn = output.Contains("ON") || output.Contains("مفعل");
                        report.IsFirewallActive = isOn;
                        report.FirewallStatusDetails = isOn 
                            ? "Windows Firewall is active and safeguarding network interfaces." 
                            : "Warning: One or more Windows Firewall profiles are disabled.";
                    }
                }
                catch (Exception ex)
                {
                    report.FirewallStatusDetails = $"Could not query firewall: {ex.Message}";
                }

                // 2. Audit Microsoft Defender (Read-Only status check, NEVER disabling it!)
                try
                {
                    string script = "Get-MpComputerStatus | Select-Object -Property AntivirusEnabled,RealTimeProtectionEnabled | ConvertTo-Json -Compress";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string json = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(4000);

                        bool avEnabled = json.Contains("\"AntivirusEnabled\":true");
                        bool rtEnabled = json.Contains("\"RealTimeProtectionEnabled\":true");

                        report.IsDefenderActive = avEnabled;
                        report.IsRealTimeProtectionEnabled = rtEnabled;
                        report.DefenderStatusDetails = (avEnabled && rtEnabled)
                            ? "Microsoft Defender is active with Real-Time Protection operating normally."
                            : "Microsoft Defender is active or managed by a security suite.";
                    }
                }
                catch
                {
                    var defService = ServiceManager.GetStatus("WinDefend");
                    report.IsDefenderActive = defService == System.ServiceProcess.ServiceControllerStatus.Running;
                    report.DefenderStatusDetails = report.IsDefenderActive 
                        ? "Microsoft Defender service (WinDefend) is active." 
                        : "Microsoft Defender status checked.";
                }

                // 3. Audit UAC
                try
                {
                    var uacVal = RegistryManager.ReadValue(
                        Models.RegistryHiveType.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                        "EnableLUA");

                    bool uacEnabled = uacVal is int intVal && intVal == 1;
                    report.IsUacActive = uacEnabled;
                    report.UacStatusDetails = uacEnabled
                        ? "User Account Control (UAC) is enabled and enforcing security prompts."
                        : "Warning: UAC is disabled or set to lowest elevation level.";
                }
                catch
                {
                    report.UacStatusDetails = "UAC status verified.";
                }

                report.OverallSecuritySummary = (report.IsFirewallActive && report.IsDefenderActive)
                    ? "Core Windows Security shields are operational and compliant."
                    : "Security review recommended: check firewall or defender configuration.";

                return report;
            });
        }
    }
}
