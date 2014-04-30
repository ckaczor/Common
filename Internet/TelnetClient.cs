using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using Common.Debug;

namespace Common.Internet
{
	public class TelnetClient
	{
		#region Member variables

		private string _deviceLocation = "";								// Name or IP address of the device
		private int _devicePort = 80;										// Port the device is listening on
		private SocketState _state;									        // Async state tracking object
		private Socket _socket;										        // Socket for communications with the device

		#endregion

		#region Asynchronous socket state

		private class SocketState
		{
            private readonly Socket _socket;                                // Network socket
			private readonly byte[] _buffer;								// Raw data buffer
			private readonly int _bufferSize = 1024;						// Data buffer size
			private readonly TelnetClient _client;							// Owner client
			private readonly StringBuilder _stringBuffer;					// Buffered data
			private readonly ManualResetEvent _waitEvent;					// Used as a signal when waiting for data

			public SocketState(Socket socket, TelnetClient client, int bufferSize)
			{
				// Store the socket
				_socket = socket;

				// Store the client
				_client = client;

				// Store the buffer size
				_bufferSize = bufferSize;

				// Initialize the buffer
				_buffer = new byte[_bufferSize];

				// Initialize the string buffer
				_stringBuffer = new StringBuilder();

				// Initialize the wait event
				_waitEvent = new ManualResetEvent(false);
			}

			public int BufferSize
			{
				get
				{
					return _bufferSize;
				}
			}

			public Socket Socket
			{
				get
				{
					return _socket;
				}
			}

			public TelnetClient Client
			{
				get
				{
					return _client;
				}
			}

			public byte[] Buffer
			{
				get
				{
					return _buffer;
				}
			}

			public StringBuilder StringBuffer
			{
				get
				{
					return _stringBuffer;
				}
			}

			public ManualResetEvent WaitEvent
			{
				get
				{
					return _waitEvent;
				}
			}
		}

		#endregion

		#region Delegates

		public delegate void DataEventHandler(string data);

		#endregion

		#region Events

		public event DataEventHandler DataReceived;

		#endregion

		#region Connection

		public void Connect(string location, int port)
		{
			// Remember the location and port
			_deviceLocation = location;
			_devicePort = port;

			// Create the socket
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			// Connect the socket
			_socket.Connect(_deviceLocation, _devicePort);

			// Setup our state object
			_state = new SocketState(_socket, this, 1024);

			// Start an asynchronous receive
			_socket.BeginReceive(_state.Buffer, 0, _state.BufferSize, SocketFlags.None, new AsyncCallback(receiveCallback), _state);
		}

		#endregion

		#region Disconnection

		public void Disconnect()
		{
			// If we have a socket...
			if (null != _socket)
			{
				// ...disconnect it
				_socket.Disconnect(false);
				_socket = null;
			}
		}

		#endregion

		#region Asynchronous callback

		private static void receiveCallback(IAsyncResult asyncResult)
		{
			// Retrieve the socket state object
			SocketState socketState = (SocketState) asyncResult.AsyncState;

			// Read data from the socket
			int bytesRead = socketState.Socket.EndReceive(asyncResult);

			if (bytesRead > 0)
			{
				// Decode the data into a string
				string stringBuffer = Encoding.UTF8.GetString(socketState.Buffer, 0, bytesRead);

				// Add the string to the buffer
				socketState.StringBuffer.Append(stringBuffer);

				// Raise the DataReceived event with the data
				if (socketState.Client.DataReceived != null)
					socketState.Client.DataReceived(stringBuffer);

				Tracer.WriteLine("Data received: {0}", stringBuffer);

				socketState.WaitEvent.Set();

				// Get more data from the socket
				socketState.Socket.BeginReceive(socketState.Buffer, 0, socketState.BufferSize, 0, new AsyncCallback(receiveCallback), socketState);
			}
		}

		#endregion

		#region Data sending helpers

		public void Send(string text, params object[] arguments)
		{
			// Format the text
			text = string.Format(text, arguments);

			Tracer.WriteLine("Send: {0}", text);

			// Send the text to the socket
			_socket.Send(Encoding.UTF8.GetBytes(text));
		}

		public void SendLine(string text, params object[] arguments)
		{
			// Send the text with a newline
			Send(text + "\n", arguments);
		}

		#endregion

		#region Data retrieval helpers

		public string WaitForLine(string text)
		{
			// Add a CR/LF to the string and start waiting
			return WaitForString(text + "\r\n");
		}

		public string WaitForString(string text)
		{
			// Check to see if the current buffer contains the text
			while (!_state.StringBuffer.ToString().Contains(text))
			{
				Tracer.WriteLine("Waiting for: {0}", text);

				// Wait for more data
				_state.WaitEvent.Reset();
				_state.WaitEvent.WaitOne(1000, false);
			}

			Tracer.WriteLine("String found: {0}", text);

			// Get the text in the buffer from the start to after the string we are waiting for
			string returnString = _state.StringBuffer.ToString().Substring(0, _state.StringBuffer.ToString().IndexOf(text) + text.Length);

			// Strip off everything before what we are waiting for
			_state.StringBuffer.Remove(0, _state.StringBuffer.ToString().IndexOf(text) + text.Length);

			// Return the string
			return returnString;
		}

		public string ReadLine()
		{
		    // Wait for the first end of line
			string line = WaitForString("\r\n");

			// Get rid of the trailing CR/LF
			line = line.Substring(0, line.Length - 2);

			// Return the line
			return line;
		}

		#endregion
	}
}
