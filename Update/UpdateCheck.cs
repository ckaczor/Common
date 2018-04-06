using Common.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Common.Update
{
    public enum ServerType
    {
        Generic,
        GitHub
    }

    public static class UpdateCheck
    {
        public delegate void ApplicationShutdownDelegate();
        public delegate void ApplicationCurrentMessageDelegate(string title, string message);
        public delegate bool ApplicationUpdateMessageDelegate(string title, string message);

        public static ApplicationShutdownDelegate ApplicationShutdown = null;
        public static ApplicationCurrentMessageDelegate ApplicationCurrentMessage = null;
        public static ApplicationUpdateMessageDelegate ApplicationUpdateMessage = null;

        public static ServerType UpdateServerType;
        public static string UpdateServer;
        public static string UpdateFile;
        public static string ApplicationName { get; set; }

        public static VersionInfo RemoteVersion { get; private set; }
        public static string LocalInstallFile { get; private set; }
        public static bool UpdateAvailable { get; private set; }

        public static Version LocalVersion => Assembly.GetEntryAssembly().GetName().Version;

        public static bool CheckForUpdate()
        {
            RemoteVersion = VersionInfo.Load(UpdateServerType, UpdateServer, UpdateFile);

            if (RemoteVersion == null)
                return false;

            var serverVersion = RemoteVersion.Version;
            var localVersion = LocalVersion;

            UpdateAvailable = serverVersion > localVersion;

            return true;
        }

        public static async Task<bool> DownloadUpdate()
        {
            if (RemoteVersion == null)
                return false;

            var remoteFile = UpdateServer + RemoteVersion.InstallFile;

            LocalInstallFile = Path.Combine(Path.GetTempPath(), RemoteVersion.InstallFile);

            var webClient = new WebClient();

            await webClient.DownloadFileTaskAsync(new Uri(remoteFile), LocalInstallFile);

            return true;
        }

        public static bool InstallUpdate()
        {
            if (RemoteVersion == null)
                return false;

            Process.Start(LocalInstallFile, "/passive");

            return true;
        }

        public static async void DisplayUpdateInformation(bool showIfCurrent)
        {
            CheckForUpdate();

            // Check for an update
            if (UpdateAvailable)
            {
                // Load the version string from the server
                var serverVersion = RemoteVersion.Version;

                // Format the check title
                var updateCheckTitle = string.Format(Resources.UpdateCheckTitle, ApplicationName);

                // Format the message
                var updateCheckMessage = string.Format(Resources.UpdateCheckNewVersion, ApplicationName, serverVersion);

                // Ask the user to update
                if (ApplicationUpdateMessage(updateCheckTitle, updateCheckMessage))
                    return;

                // Get the update
                await DownloadUpdate();

                // Start to install the update
                InstallUpdate();

                // Restart the application
                ApplicationShutdown();
            }
            else if (showIfCurrent)
            {
                // Format the check title
                var updateCheckTitle = string.Format(Resources.UpdateCheckTitle, ApplicationName);

                // Format the message
                var updateCheckMessage = string.Format(Resources.UpdateCheckCurrent, ApplicationName);

                ApplicationCurrentMessage(updateCheckTitle, updateCheckMessage);
            }
        }

    }
}
