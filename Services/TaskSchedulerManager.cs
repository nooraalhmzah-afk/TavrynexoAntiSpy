using System;
using System.Diagnostics;

namespace TavrynexoAntiSpy.Services
{
    public class TaskOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class TaskSchedulerManager
    {
        public static bool TaskExists(string taskPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/query /tn \"{taskPath}\" /fo csv /nh",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                proc.WaitForExit(4000);
                return proc.ExitCode == 0;
            }
            catch { return false; }
        }

        public static bool IsTaskDisabled(string taskPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/query /tn \"{taskPath}\" /fo list /v",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);
                return output.Contains("Disabled", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static TaskOperationResult DisableTask(string taskPath)
        {
            var result = new TaskOperationResult();
            if (!TaskExists(taskPath)) return new TaskOperationResult { Success = true, Message = "Task absent." };

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/change /tn \"{taskPath}\" /disable",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(6000);
                result.Success = IsTaskDisabled(taskPath);
                result.Message = result.Success ? $"Task '{taskPath}' disabled." : "Failed to disable task.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public static TaskOperationResult EnableTask(string taskPath)
        {
            var result = new TaskOperationResult();
            if (!TaskExists(taskPath)) return new TaskOperationResult { Success = true };

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/change /tn \"{taskPath}\" /enable",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(6000);
                result.Success = !IsTaskDisabled(taskPath);
                result.Message = "Task re-enabled.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
