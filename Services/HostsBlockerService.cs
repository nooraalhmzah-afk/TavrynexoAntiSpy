using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TavrynexoAntiSpy.Services
{
    public class HostsOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class HostsBlockerService
    {
        private const string BlockHeader = "# >>> TAVRYNEXO ANTISPY TELEMETRY BLOCK >>>";
        private const string BlockFooter = "# <<< TAVRYNEXO ANTISPY TELEMETRY BLOCK <<<";

        private static readonly string HostsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers", "etc", "hosts");

        public static readonly List<string> TelemetryDomains = new()
        {
            "telemetry.microsoft.com",
            "vortex.data.microsoft.com",
            "vortex-win.data.microsoft.com",
            "telecommand.telemetry.microsoft.com",
            "oca.telemetry.microsoft.com",
            "sqm.telemetry.microsoft.com"
        };

        public static bool IsTelemetryBlocked()
        {
            try
            {
                if (!File.Exists(HostsFilePath)) return false;
                string content = File.ReadAllText(HostsFilePath);
                return content.Contains(BlockHeader);
            }
            catch { return false; }
        }

        public static HostsOperationResult BlockTelemetryDomains()
        {
            var result = new HostsOperationResult();
            try
            {
                if (!File.Exists(HostsFilePath)) return new HostsOperationResult { Success = false, Message = "Hosts missing." };
                string existing = File.ReadAllText(HostsFilePath);
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine(BlockHeader);
                foreach (var d in TelemetryDomains) sb.AppendLine($"0.0.0.0 {d}");
                sb.AppendLine(BlockFooter);
                File.WriteAllText(HostsFilePath, existing.TrimEnd() + Environment.NewLine + sb.ToString());
                FlushDns();
                result.Success = true;
                result.Message = "Telemetry domains blocked in hosts.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public static HostsOperationResult UnblockTelemetryDomains()
        {
            var result = new HostsOperationResult();
            try
            {
                if (!File.Exists(HostsFilePath)) return new HostsOperationResult { Success = true };
                string content = File.ReadAllText(HostsFilePath);
                int start = content.IndexOf(BlockHeader, StringComparison.OrdinalIgnoreCase);
                if (start != -1)
                {
                    int end = content.IndexOf(BlockFooter, StringComparison.OrdinalIgnoreCase);
                    if (end != -1) content = content.Remove(start, (end + BlockFooter.Length) - start);
                    File.WriteAllText(HostsFilePath, content.Trim() + Environment.NewLine);
                }
                FlushDns();
                result.Success = true;
                result.Message = "Hosts unblocked.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        private static void FlushDns()
        {
            try
            {
                var psi = new ProcessStartInfo { FileName = "ipconfig.exe", Arguments = "/flushdns", CreateNoWindow = true, UseShellExecute = false };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);
            }
            catch { }
        }
    }
}
