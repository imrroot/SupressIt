using System;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32;

namespace SupressIt.Helpers
{
    /// <summary>
    /// Writes / removes the app from HKCU Run key so it starts with Windows.
    /// Also provides IsAdmin check.
    /// </summary>
    public static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SupressIt";

        public static bool IsAdmin =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

        /// <summary>Add or remove the app from Windows startup via registry.</summary>
        public static void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null) return;

                if (enable)
                {
                    string exePath = Assembly.GetExecutingAssembly().Location
                        .Replace(".dll", ".exe");
                    key.SetValue(AppName, $"\"{exePath}\" --startup");
                }
                else
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
            catch { }
        }

        /// <summary>Check if app was registered to startup correctly.</summary>
        public static bool IsRegisteredForStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(AppName) != null;
            }
            catch { return false; }
        }

        /// <summary>Restart the current process as administrator.</summary>
        public static void RestartAsAdmin()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                                      ?? Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"),
                    UseShellExecute = true,
                    Verb            = "runas"
                };
                System.Diagnostics.Process.Start(psi);
                System.Windows.Application.Current.Shutdown();
            }
            catch { /* user cancelled UAC */ }
        }
    }
}
