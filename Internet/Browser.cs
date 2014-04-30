using System.Collections.Generic;
using Microsoft.Win32;

namespace Common.Internet
{
    public class Browser
    {
        public string Name { get; private set; }
        public string Command { get; private set; }
        public string DefaultIcon { get; private set; }

        public override string ToString()
        {
            return Name;
        }

        public static Dictionary<string, Browser> DetectInstalledBrowsers()
        {
            var browsers = new Dictionary<string, Browser>();

            RegistryKey browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");
            if (browserKeys == null)
                browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Clients\StartMenuInternet");                

            string[] browserNames = browserKeys.GetSubKeyNames();

            foreach (string browserName in browserNames)
            {
                Browser browser = new Browser();
                RegistryKey browserKey = browserKeys.OpenSubKey(browserName);
                browser.Name = (string) browserKey.GetValue(null);
                RegistryKey browserKeyPath = browserKey.OpenSubKey(@"shell\open\command");
                browser.Command = (string) browserKeyPath.GetValue(null);
                RegistryKey browserIconPath = browserKey.OpenSubKey(@"DefaultIcon");
                browser.DefaultIcon = (string) browserIconPath.GetValue(null);
                browsers.Add(browserName, browser);
            }

            return browsers;
        }
    }
}
