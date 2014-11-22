using Common.Debug;
using System;
using System.Xml.Linq;

namespace Common.Update
{
    public class VersionInfo
    {
        public Version Version { get; set; }
        public string InstallFile { get; set; }
        public DateTime InstallCreated { get; set; }

        public static VersionInfo Load(string server, string file)
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
    }
}
