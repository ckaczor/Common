using System;
using System.Diagnostics;
using System.Security.Principal;

namespace Common
{
    public class Security
    {
        public static void EnsureElevatedInstall()
        {
            using (var wi = WindowsIdentity.GetCurrent())
            {
                if (wi == null)
                    return;

                var wp = new WindowsPrincipal(wi);

                if (wp.IsInRole(WindowsBuiltInRole.Administrator))
                    return;

                using (var currentProc = Process.GetCurrentProcess())
                using (var proc = new Process())
                {
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.FileName = currentProc.MainModule.FileName;
                    proc.StartInfo.Arguments = "/reinstall";
                    proc.Start();
                }

                Environment.Exit(0);
            }
        }
    }
}
