using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.Internet
{
    public class FtpClient
    {
        #region Private constants

        private const int DATA_SIZE = 4096;

        #endregion

        #region Private member variables

        private Socket _controlSocket;      // The socket for sending control messsages
        private Socket _dataSocket;         // The socket for transferring data
        private int _timeout = 30000;       // The default send and receive timeouts

        #endregion

        #region Private socket management code

        private Socket connectSocket(string server, int port)
        {
            // Resolve the server name into an IP Host
            IPHostEntry ipHost = Dns.GetHostEntry(server);

            // Get the first address from the host
            IPAddress ipAddress = ipHost.AddressList[0];

            // Create an end point on the host at the requested port
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            // Create a TCP socket to the end point on the host
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Set the send and receive timeouts requested
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);

            // Make the connection
            socket.Connect(ipEndPoint);

            return socket;
        }

        private static void disconnectSocket(ref Socket targetSocket)
        {
            if (targetSocket != null)
            {
                targetSocket.Shutdown(SocketShutdown.Both);
                targetSocket.Close();
                targetSocket = null;
            }
        }

        private static void sendSocketMessage(Socket targetSocket, string message)
        {
            sendSocketMessage(targetSocket, message, true);
        }

        private static void sendSocketMessage(Socket targetSocket, string message, bool trailingNewline)
        {
            // Default to sending just the message
            string send = message;

            // If a trailing newline is requested then add that to the string
            if (trailingNewline) send += "\r\n";

            // Convert the requested message into a byte array
            byte[] rawData = Encoding.ASCII.GetBytes(send);

            // Send the message and get the byte count back
            targetSocket.Send(rawData);
        }

        private static string receiveSocketMessage(Socket sourceSocket, bool stripTrailingNewline)
        {
            // Initialize the decoded string message to return
            string message = string.Empty; 

            // Initialize the array of all lines
            string[] lines;

            do
            {
                // Initialize the byte array buffer
                byte[] line = new byte[DATA_SIZE];

                // Retrieve the data and get the byte count
                int bytesReceived = sourceSocket.Receive(line);

                // Decode the string into a string for the current line
                string sLine = Encoding.ASCII.GetString(line, 0, bytesReceived);

                // Replace the CR/LF pairs with just a LF
                sLine.Replace("\r\n", "\n");

                // Split the message into an array of lines (we might get more than one)
                lines = sLine.Split('\n');

                // Add this line to the main return string
                message += sLine;
            } while (lines[lines.Length - 2].Substring(3, 1) == "-");

            // If requested to remove the trailing newline we should do it now
            if (stripTrailingNewline && message.EndsWith("\n"))
            {
                message = message.Remove(message.Length - 1, 1);
            }

            return message;
        }

        public int GetResponseCode(string message)
        {
            // Get the FTP response code from the string message
            return Convert.ToInt32(message.Substring(1, 3));
        }

        #endregion

        #region Public interface methods

        public void Connect(string server)
        {
            Connect(server, 21, _timeout);
        }

        public void Connect(string server, int port, int timeout)
        {
            // Store the timeout requested so we can use it later
            _timeout = timeout * 1000;

            // If we are currently connected in any way we should disconnect
            if (_controlSocket != null || _dataSocket != null) Disconnect();

            // Create and connect the control socket
            _controlSocket = connectSocket(server, port);

            // Retrieve the header message
            receiveSocketMessage(_controlSocket, true);
        }

        public void Upload(string source)
        {
            string targetFile;

            if (source.Contains(@"\"))
                targetFile = source.Substring(source.LastIndexOf(@"\") + 1);
            else
                targetFile = source;

            // Create a stream reader for the file
            StreamReader streamReader = new StreamReader(source);

            // Upload the file
            Upload(streamReader.BaseStream, targetFile);
        }

        public void Upload(Stream source, string targetFile)
        {
            // Send a command requesting the passive port to connect to
            sendSocketMessage(_controlSocket, "PASV");

            // Get the response back with the data we need
            string message = receiveSocketMessage(_controlSocket, true);

            message = message.Replace("(", "<");
            message = message.Replace(")", ">");

            // Create a regular expression to get the port data out
            Match match = new Regex("<(?<oct1>.*),(?<oct2>.*),(?<oct3>.*),(?<oct4>.*),(?<highport>.*),(?<lowport>.*)>").Match(message);

            // Reconstruct the IP address the server wants
            string address = match.Groups["oct1"].Value + "." + match.Groups["oct2"].Value + "." +
                             match.Groups["oct3"].Value + "." + match.Groups["oct4"].Value;

            // Reconstruct the port the server wants
            int port = Convert.ToInt32(match.Groups["highport"].Value) * 256 +
                       Convert.ToInt32(match.Groups["lowport"].Value);

            // Connect the socket to that port
            _dataSocket = connectSocket(address, port);

            // Send the command requesting to store a file
            sendSocketMessage(_controlSocket, "STOR " + targetFile);

            // Get the response back
            receiveSocketMessage(_controlSocket, false);

            // Bring the stream to the beginning if it supports seek
            if (source.CanSeek) source.Seek(0, SeekOrigin.Begin);

            // Loop until the end of the file
            while (source.Length != source.Position)
            {
                // Initialize the byte array buffer
                byte[] rawMessage = new byte[DATA_SIZE];

                // Get data from the stream
                int bytesRead = source.Read(rawMessage, 0, DATA_SIZE);

                // Send the data down the data socket
                _dataSocket.Send(rawMessage, bytesRead, 0);
            }

            // Close the data socket
            disconnectSocket(ref _dataSocket);

            // Get the response back
            receiveSocketMessage(_controlSocket, false);
        }

        public void Download(string sourceFile, string targetFile)
        {
        }

        public void Logon(string userName, string password)
        {
            // Send the command to indicate the user we want
            sendSocketMessage(_controlSocket, "USER " + userName);

            receiveSocketMessage(_controlSocket, true);

            // Send the command to indicate the password we want
            sendSocketMessage(_controlSocket, "PASS " + password);

            receiveSocketMessage(_controlSocket, true);
        }

        public void Logoff()
        {
            // Send the comment to close the current session
            sendSocketMessage(_controlSocket, "QUIT");

            receiveSocketMessage(_controlSocket, true);
        }

        public void Disconnect()
        {
            // Close and clear the data socket
            disconnectSocket(ref _dataSocket);

            // Close and clear the control socket
            disconnectSocket(ref _controlSocket);
        }

        #endregion
    }
}