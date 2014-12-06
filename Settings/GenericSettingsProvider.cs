using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace Common.Settings
{
    public class GenericSettingsProvider : SettingsProvider, IApplicationSettingsProvider
    {
        #region Member variables

        private string _applicationName = string.Empty;

        #endregion

        #region Delegates

        public delegate object OpenDataStoreDelegate();
        public delegate void CloseDataStoreDelegate(object dataStore);
        public delegate string GetSettingValueDelegate(object dataStore, string name, string version);
        public delegate void SetSettingValueDelegate(object dataStore, string name, string version, string value);
        public delegate List<string> GetVersionListDelegate(object dataStore);
        public delegate void DeleteSettingsForVersionDelegate(object dataStore, string version);

        #endregion

        #region Callbacks

        public OpenDataStoreDelegate OpenDataStore = null;
        public CloseDataStoreDelegate CloseDataStore = null;
        public GetSettingValueDelegate GetSettingValue = null;
        public SetSettingValueDelegate SetSettingValue = null;
        public GetVersionListDelegate GetVersionList = null;
        public DeleteSettingsForVersionDelegate DeleteSettingsForVersion = null;

        #endregion

        #region SettingsProvider members

        public override string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value; }
        }

        public bool DeleteOldVersionsOnUpgrade { get; set; }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (string.IsNullOrEmpty(name))
                name = GetType().Name;

            base.Initialize(name, config);
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            // Create a new collection for the values
            var values = new SettingsPropertyValueCollection();

            // Get the current version number
            var version = GetCurrentVersionNumber();

            // Open the data store
            var dataStore = OpenDataStore();

            // Loop over each property
            foreach (SettingsProperty property in properties)
            {
                // Get the setting value for the current version
                var value = GetPropertyValue(dataStore, property, version);

                // Add the value to the collection
                values.Add(value);
            }

            // Close the data store
            CloseDataStore(dataStore);

            return values;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection properties)
        {
            // Get the current version number
            var version = GetCurrentVersionNumber();

            // Open the data store
            var dataStore = OpenDataStore();

            // Loop over each property
            foreach (SettingsPropertyValue propertyValue in properties)
            {
                // If the property isn't dirty or it is null then we can skip it
                if (!propertyValue.IsDirty || (propertyValue.SerializedValue == null))
                    continue;

                // Set the property value
                SetPropertyValue(dataStore, propertyValue, version);
            }

            // Close the data store
            CloseDataStore(dataStore);
        }

        #endregion

        #region Version numbers

        private static string GetCurrentVersionNumber()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }

        private string GetPreviousVersionNumber(object dataStore)
        {
            // Get the current version number
            var currentVersion = GetCurrentVersionNumber();

            // Get a distinct list of version numbers 
            var versionList = GetVersionList(dataStore);

            // Remove the current version
            versionList.Remove(currentVersion);

            // Remove the empty version for the database version
            versionList.Remove(string.Empty);

            // Sort the list using the Version object and get the first value
            var previousVersion = versionList.OrderByDescending(v => new Version(v)).FirstOrDefault();

            return previousVersion;
        }

        #endregion

        #region Value get and set

        private SettingsPropertyValue GetPropertyValue(object dataStore, SettingsProperty property, string version)
        {
            // Create the value for the property
            var value = new SettingsPropertyValue(property);

            // Try to get the setting that matches the name and version
            var setting = GetSettingValue(dataStore, property.Name, version);

            // If the setting was found then set the value, otherwise leave as default
            value.SerializedValue = setting;

            // Value is not dirty since we just read it
            value.IsDirty = false;

            return value;
        }

        private void SetPropertyValue(object dataStore, SettingsPropertyValue value, string version)
        {
            // Set the value for this version
            SetSettingValue(dataStore, value.Property.Name, version, value.SerializedValue.ToString());
        }

        #endregion

        #region IApplicationSettingsProvider members

        public void Reset(SettingsContext context)
        {
            // Get the current version number
            var version = GetCurrentVersionNumber();

            // Open the data store
            var dataStore = OpenDataStore();

            // Delete all settings for this version
            DeleteSettingsForVersion(dataStore, version);

            // Close the data store
            CloseDataStore(dataStore);
        }

        public SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property)
        {
            // Open the data store
            var dataStore = OpenDataStore();

            // Get the previous version number
            var previousVersion = GetPreviousVersionNumber(dataStore);

            SettingsPropertyValue value;

            // If there is no previous version we have a return a setting with a null value
            if (string.IsNullOrEmpty(previousVersion))
            {
                // Create a new property value with the value set to null
                value = new SettingsPropertyValue(property) { PropertyValue = null };

                return value;
            }

            // Return the value from the previous version
            value = GetPropertyValue(dataStore, property, previousVersion);

            // Close the data store
            CloseDataStore(dataStore);

            return value;
        }

        public void Upgrade(SettingsContext context, SettingsPropertyCollection properties)
        {
            // Open the data store
            var dataStore = OpenDataStore();

            if (dataStore == null)
                return;

            // Get the previous version number
            var previousVersion = GetPreviousVersionNumber(dataStore);

            // If there is no previous version number just do nothing
            if (string.IsNullOrEmpty(previousVersion))
                return;

            // Delete everything for the current version
            Reset(context);

            // Get the current version number
            var currentVersion = GetCurrentVersionNumber();

            // Loop over each property
            foreach (SettingsProperty property in properties)
            {
                // Get the previous value
                var previousValue = GetPropertyValue(dataStore, property, previousVersion);

                // Set the current value if there was a previous value
                if (previousValue.SerializedValue != null)
                    SetPropertyValue(dataStore, previousValue, currentVersion);
            }

            if (DeleteOldVersionsOnUpgrade)
            {
                // Get a distinct list of version numbers 
                var versionList = GetVersionList(dataStore);

                // Remove the current version
                versionList.Remove(currentVersion);

                // Delete settings for anything left
                foreach (var version in versionList)
                    DeleteSettingsForVersion(dataStore, version);
            }

            // Close the data store
            CloseDataStore(dataStore);
        }

        #endregion

        #region Setting scope helpers

        // ReSharper disable once UnusedMember.Local
        private bool IsApplicationScoped(SettingsProperty property)
        {
            return HasSettingScope(property, typeof(ApplicationScopedSettingAttribute));
        }

        // ReSharper disable once UnusedMember.Local
        private bool IsUserScoped(SettingsProperty property)
        {
            return HasSettingScope(property, typeof(UserScopedSettingAttribute));
        }

        private bool HasSettingScope(SettingsProperty property, Type attributeType)
        {
            // Check if the setting is application scoped
            var isApplicationScoped = property.Attributes[typeof(ApplicationScopedSettingAttribute)] != null;

            // Check if the setting is user scoped
            var isUserScoped = property.Attributes[typeof(UserScopedSettingAttribute)] != null;

            // Both user and application is not allowed
            if (isUserScoped && isApplicationScoped)
                throw new Exception("Setting cannot be both application and user scoped: " + property.Name);

            // Must be set to either user or application 
            if (!isUserScoped && !isApplicationScoped)
                throw new Exception("Setting must be either application or user scoped: " + property.Name);

            // If we want to know if it is application scoped return that value
            if (attributeType == typeof(ApplicationScopedSettingAttribute))
                return isApplicationScoped;

            // If we want to know if it is user scoped return that value
            if (attributeType == typeof(UserScopedSettingAttribute))
                return isUserScoped;

            return false;
        }

        #endregion
    }
}
