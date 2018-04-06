using Common.Debug;
using System;
using System.Net;
using System.Xml.Linq;

namespace Common.Update
{
    public class VersionInfo
    {
        public Version Version { get; set; }
        public string InstallFile { get; set; }
        public DateTime InstallCreated { get; set; }

        public static VersionInfo Load(ServerType updateServerType, string server, string file)
        {
            switch (updateServerType)
            {
                case ServerType.Generic:
                    return LoadFile(server, file);
                case ServerType.GitHub:
                    return LoadGitHub(server);
            }

            return null;
        }

        private static VersionInfo LoadFile(string server, string file)
        {
            try
            {
                var document = XDocument.Load(server + file);

                var versionInformationElement = document.Element("versionInformation");

                if (versionInformationElement == null)
                    return null;

                var versionElement = versionInformationElement.Element("version");
                var installFileElement = versionInformationElement.Element("installFile");
                var installCreatedElement = versionInformationElement.Element("installCreated");

                if (versionElement == null || installFileElement == null || installCreatedElement == null)
                    return null;

                var versionInfo = new VersionInfo
                {
                    Version = Version.Parse(versionElement.Value),
                    InstallFile = installFileElement.Value,
                    InstallCreated = DateTime.Parse(installCreatedElement.Value)
                };

                return versionInfo;
            }
            catch (Exception exception)
            {
                Tracer.WriteException(exception);

                return null;
            }
        }

        private static VersionInfo LoadGitHub(string server)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("User-Agent", UpdateCheck.ApplicationName);

                    var json = webClient.DownloadString(server);

                    var release = GitHubRelease.FromJson(json);

                    var versionInfo = new VersionInfo
                    {
                        Version = Version.Parse(release.TagName),
                        InstallFile = release.Assets[0].BrowserDownloadUrl,
                        InstallCreated = release.Assets[0].CreatedAt.LocalDateTime
                    };

                    return versionInfo;
                }
            }
            catch (Exception exception)
            {
                Tracer.WriteException(exception);

                return null;
            }
        }
    }
}
