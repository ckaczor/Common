using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Common.Internet
{
    public class Browser
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        public string Key { get; private set; }
        public string Name { get; private set; }
        public string Command { get; private set; }
        public string DefaultIcon { get; private set; }
        public ImageSource DefaultImage { get; private set; }
        public bool SupportsPrivate { get; private set; }

        public void LoadImage()
        {
            var parts = DefaultIcon.Split(',');
            var index = int.Parse(parts[1]);

            var large = new IntPtr[1];
            var small = new IntPtr[1];
            ExtractIconEx(parts[0], index, large, small, 1);

            Icon img = Icon.FromHandle(small[0]);

            Bitmap bitmap = img.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            DefaultImage = wpfBitmap;
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
            return DetectInstalledBrowsers(false, false);
        }

        public static Dictionary<string, Browser> DetectInstalledBrowsers(bool loadImages, bool includeUseSystemDefault)
        {
            var browsers = new Dictionary<string, Browser>();

            if (includeUseSystemDefault)
            {
                var browser = new Browser
                {
                    Key = string.Empty,
                    Name = "System Default",
                    SupportsPrivate = false
                };

                browsers.Add(browser.Key, browser);
            }

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
                    DefaultIcon = browserIconPath == null ? null : (string) browserIconPath.GetValue(null),
                    SupportsPrivate = true
                };

                if (loadImages)
                    browser.LoadImage();

                browsers.Add(browserName, browser);
            }

            return browsers;
        }
    }
}
