using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TavrynexoAntiSpy.Services
{
    public static class RestorePointService
    {
        public static async Task<(bool Success, string Message)> CreateRestorePointAsync(string description = "Tavrynexo AntiSpy Baseline")
        {
            return await Task.Run(() =>
            {
                try
                {
                    string script = $"Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var process = Process.Start(psi);
                    process?.WaitForExit(35000);
                    bool ok = process?.ExitCode == 0;
                    return (ok, ok ? "System Restore Point created successfully." : "Restore Point creation failed.");
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });
        }
    }
}
