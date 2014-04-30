using System.Configuration;
using System.IO;

using Common.Debug;

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
            if (configFile.Directory == null || configFile.Directory.Parent == null)
                return;

            // Loop over all directories of the configuration file parent directory
            foreach (DirectoryInfo directory in configFile.Directory.Parent.GetDirectories())
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
    }
}
