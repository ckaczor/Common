using System;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Common.Helpers
{
    public static class ApplicationIsolation
    {
        public static IDisposable GetIsolationHandle()
        {
            string applicationName = Assembly.GetEntryAssembly().GetName().Name;

            return GetIsolationHandle(applicationName);
        }

        public static IDisposable GetIsolationHandle(string applicationName)
        {
            // Create mutex security settings
            MutexSecurity mutexSecurity = new MutexSecurity();

            // Create a world security identifier
            SecurityIdentifier securityIdentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

            // Create an access rule for the mutex
            MutexAccessRule mutexAccessRule = new MutexAccessRule(securityIdentifier, MutexRights.FullControl, AccessControlType.Allow);

            // Add the access rule to the mutex security settings
            mutexSecurity.AddAccessRule(mutexAccessRule);

            // Create the mutex and see if it was newly created
            bool createdNew;
            Mutex mutex = new Mutex(false, applicationName, out createdNew, mutexSecurity);

            // Return the mutex if it is new
            return createdNew ? mutex : null;
        }
    }
}
