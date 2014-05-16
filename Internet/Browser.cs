using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Common.Internet
{
    public class Browser
    {
        public string Key { get; private set; }
        public string Name { get; private set; }
        public string Command { get; private set; }
        public string DefaultIcon { get; private set; }

        public bool SupportsPrivate()
        {
            return false;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool OpenLink(string url, bool privateMode)
        {
            // Don't bother with empty links
            if (String.IsNullOrEmpty(url))
                return true;

            // Add quotes around the URL for safety
            url = string.Format("\"{0}\"", url);

            if (privateMode)
                url += " -incognito";

            // Start the browser
            Process.Start(Command, url);

            return true;
        }

        public static Dictionary<string, Browser> DetectInstalledBrowsers()
        {
            var browsers = new Dictionary<string, Browser>();

            var browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");

            if (browserKeys == null)
                browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Clients\StartMenuInternet");

            if (browserKeys == null)
                return browsers;

            var browserNames = browserKeys.GetSubKeyNames();

            foreach (var browserName in browserNames)
            {
                var browserKey = browserKeys.OpenSubKey(browserName);

                if (browserKey == null)
                    continue;

                var browserKeyPath = browserKey.OpenSubKey(@"shell\open\command");

                if (browserKeyPath == null)
                    continue;

                var browserIconPath = browserKey.OpenSubKey(@"DefaultIcon");

                var browser = new Browser
                {
                    Key = browserName,
                    Name = (string) browserKey.GetValue(null),
                    Command = (string) browserKeyPath.GetValue(null),
                    DefaultIcon = browserIconPath == null ? null : (string) browserIconPath.GetValue(null)
                };

                browsers.Add(browserName, browser);
            }

            return browsers;
        }
    }
}
