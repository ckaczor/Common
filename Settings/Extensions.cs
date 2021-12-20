using Common.Debug;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Common.Settings
{
    public static class Extensions
    {
        public static void DeleteOldConfigurationFiles(this ApplicationSettingsBase dataSet)
        {
            // Get information about where the current configuration data is stored
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

            // Create a file object for the configuation file
            FileInfo configFile = new FileInfo(config.FilePath);

            // If we can't get the parent directories then don't bother
            if (configFile.Directory?.Parent == null)
                return;

            // Loop over all directories of the configuration file parent directory
            foreach (var directory in configFile.Directory.Parent.GetDirectories())
            {
                // Delete the directory if it isn't the current config file directory
                if (directory.FullName != configFile.Directory.FullName)
                {
                    try
                    {
                        directory.Delete(true);
                    }
                    catch (IOException exception)
                    {
                        // Another instance is probably accessing it - this directory will be deleted later on another run
                        Tracer.WriteException(exception);
                    }
                }
            }
        }

        public static void BackupSettings()
        {
            var settingsFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

            if (File.Exists(settingsFile))
            {
                var destination = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\last.config";
                File.Copy(settingsFile, destination, true);
            }
        }

        public static void RestoreSettings()
        {
            var destFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            var sourceFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\last.config";

            if (!File.Exists(sourceFile))
                return;

            var destDirectory = Path.GetDirectoryName(destFile);

            if (destDirectory == null)
                return;

            try
            {
                Directory.CreateDirectory(destDirectory);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            try
            {
                File.Copy(sourceFile, destFile, true);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            try
            {
                File.Delete(sourceFile);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
