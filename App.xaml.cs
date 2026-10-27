using System.Security.Principal;
using System.Windows;

namespace TavrynexoAntiSpy
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!IsRunningAsAdministrator())
            {
                var result = MessageBox.Show(
                    "Tavrynexo AntiSpy requires Administrator privileges to modify Windows Registry telemetry keys and manage system services.\n\n" +
                    "Do you want to continue in Read-Only Audit mode?",
                    "Administrator Privileges Required - Tavrynexo AntiSpy",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    Shutdown();
                    return;
                }
            }
        }

        public static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
