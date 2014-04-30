using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

namespace Common.IO
{
    #region Sender

    /// <summary>
    /// Helper class for sending a simple interprocess string message
    /// </summary>
    public static class InterprocessMessageSender
    {
        public static void SendMessage(string message)
        {
            string applicationName = Assembly.GetEntryAssembly().GetName().Name;

            SendMessage(applicationName, message);
        }

        public static void SendMessage(string applicationName, string message)
        {
            // Create the client channel
            IpcClientChannel clientChannel = new IpcClientChannel();

            // Register the client chanel
            ChannelServices.RegisterChannel(clientChannel, false);

            // Build a URL string for the remote listener
            string messageUrl = string.Format("ipc://{0}/MessageListener", applicationName);

            // Get an instance of the remote listener
            InterprocessMessageListener messageListener = (InterprocessMessageListener) Activator.GetObject(typeof(InterprocessMessageListener), messageUrl);

            // Inform the remote object of the message
            messageListener.HandleMessage(message);

            // Unregister the channel
            ChannelServices.UnregisterChannel(clientChannel);
        }
    }

    #endregion

    #region Listener

    /// <summary>
    /// Helper class for receiving a simple interprocess string message
    /// </summary>
    public class InterprocessMessageListener : MarshalByRefObject
    {
        #region Event argument

        public class InterprocessMessageEventArgs : EventArgs
        {
            public string Message { get; private set; }

            public InterprocessMessageEventArgs(string message)
            {
                Message = message;
            }
        }

        #endregion

        // The synchronization context of the thread that requested the event
        private readonly SynchronizationContext _syncContext;

        // Event to be raised when a message has been received
        public event EventHandler<InterprocessMessageEventArgs> MessageReceived = delegate { };

        public override object InitializeLifetimeService()
        {
            // Keep the listener around forever
            return null;
        }

        public InterprocessMessageListener(string applicationName)
        {
            // Store the synchronization context of the current thread
            _syncContext = SynchronizationContext.Current;

            // Create and register an IPC channel
            IpcServerChannel serverChannel = new IpcServerChannel(applicationName);
            ChannelServices.RegisterChannel(serverChannel, false);

            // Expose the message listener for remoting
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(InterprocessMessageListener), "MessageListener", WellKnownObjectMode.Singleton);
            RemotingServices.Marshal(this, "MessageListener");
        }

        public void HandleMessage(string message)
        {
            // Fire the event on the original thread
            _syncContext.Send(delegate
                                  {
                                      // Raise an event with the contents of the message
                                      MessageReceived(this, new InterprocessMessageEventArgs(message));
                                  }, message);
        }
    }

    #endregion
}