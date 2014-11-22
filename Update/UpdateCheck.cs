using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Common.Update
{
    public static class UpdateCheck
    {
        private static string _server;

        public static VersionInfo VersionInfo { get; private set; }
        public static string LocalInstallFile { get; private set; }
        public static bool UpdateAvailable { get; private set; }

        public static Version CurrentVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public static bool CheckForUpdate(string server, string file)
        {
            _server = server;

            VersionInfo = VersionInfo.Load(_server, file);

            if (VersionInfo == null)
                return false;

            var serverVersion = VersionInfo.Version;
            var localVersion = CurrentVersion;

            UpdateAvailable = serverVersion > localVersion;

            return true;
        }

        public static async Task<bool> DownloadUpdate()
        {
            if (VersionInfo == null)
                return false;

            var remoteFile = _server + VersionInfo.InstallFile;

            LocalInstallFile = Path.Combine(Path.GetTempPath(), VersionInfo.InstallFile);

            var webClient = new WebClient();

            await webClient.DownloadFileTaskAsync(new Uri(remoteFile), LocalInstallFile);

            return true;
        }

        public static bool InstallUpdate()
        {
            if (VersionInfo == null)
                return false;

            Process.Start(LocalInstallFile, "/passive");

            return true;
        }
    }
}
