using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace TavrynexoAntiSpy.Services
{
    public class ServiceOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class ServiceManager
    {
        public static bool ServiceExists(string serviceName)
        {
            try
            {
                var services = ServiceController.GetServices();
                return services.Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            }
            catch { return false; }
        }

        public static ServiceControllerStatus? GetStatus(string serviceName)
        {
            try
            {
                if (!ServiceExists(serviceName)) return null;
                using var sc = new ServiceController(serviceName);
                return sc.Status;
            }
            catch { return null; }
        }

        public static bool IsServiceDisabled(string serviceName)
        {
            try
            {
                var startVal = RegistryManager.ReadValue(
                    Models.RegistryHiveType.LocalMachine, 
                    $@"SYSTEM\CurrentControlSet\Services\{serviceName}", 
                    "Start");
                return startVal is int intVal && intVal == 4;
            }
            catch { return false; }
        }

        public static ServiceOperationResult DisableAndStopService(string serviceName)
        {
            var result = new ServiceOperationResult();
            if (!ServiceExists(serviceName))
            {
                result.Success = true;
                result.Message = $"Service '{serviceName}' is absent.";
                return result;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"config \"{serviceName}\" start=disabled",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var p = Process.Start(psi)) p?.WaitForExit(5000);

                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                }

                result.Success = IsServiceDisabled(serviceName);
                result.Message = result.Success ? $"Service '{serviceName}' stopped & disabled." : "Could not disable service.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public static ServiceOperationResult EnableAndStartService(string serviceName, string startMode = "auto")
        {
            var result = new ServiceOperationResult();
            if (!ServiceExists(serviceName)) return new ServiceOperationResult { Success = true };

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"config \"{serviceName}\" start={startMode}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var p = Process.Start(psi)) p?.WaitForExit(5000);

                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                }
                result.Success = true;
                result.Message = $"Service '{serviceName}' enabled and started.";
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
