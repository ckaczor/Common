using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Common.Debug
{
    public static class Tracer
    {
        #region File creation date comparer

        public class FileCreationTimeComparer : IComparer<string>
        {
            #region IComparer<string> Members

            public int Compare(string x, string y)
            {
                DateTime createdX = File.GetCreationTime(x);
                DateTime createdY = File.GetCreationTime(y);

                return createdY.CompareTo(createdX);
            }

            #endregion
        }

        #endregion

        #region Enumerations

        public enum IndentOption
        {
            IncrementIndentLevel,
            DecrementIndentLevel,
        }

        #endregion

        #region Member variables

        private static string _mainFileName = "Trace.txt";
        private static int _indentLevel;
        private static bool _initialized;
        private static TextWriterTraceListener _traceListener;
        private static int _keepRevisions = 3;
        private static string _outputTemplate = "[{0:MM/dd/yyyy HH:mm:ss.fff} - 0x{1:x3}] {2}{3}";
        private static string _fileNameTemplate = "{0}_{1}.txt";
        private static bool _echoToConsole;

        #endregion

        #region Methods

        [Obsolete]
        public static void Initialize()
        {
            // Initialize using some generic defaults
            Initialize(Path.GetDirectoryName(Application.ExecutablePath), "Trace", Process.GetCurrentProcess().Id.ToString(), false);
        }

        public static void Initialize(string logPath, string rootName, string uniqueId, bool echoToConsole)
        {
            _echoToConsole = echoToConsole;

            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            if (!string.IsNullOrEmpty(logPath))
            {
                // Use the file name template build the base file name
                _mainFileName = string.Format(_fileNameTemplate, rootName, uniqueId);

                // Get the list of old  log files
                string[] oldLogFiles = Directory.GetFiles(logPath, string.Format(_fileNameTemplate, rootName, "*"), SearchOption.TopDirectoryOnly);

                // Sort the list by creation date
                Array.Sort(oldLogFiles, new FileCreationTimeComparer());

                // Keep only the last X revisions
                for (int i = _keepRevisions; i < oldLogFiles.Length; i++)
                {
                    // Delete the file
                    File.Delete(oldLogFiles[i]);
                }

                // Add the log path 
                _mainFileName = Path.Combine(logPath, _mainFileName);

                // Create the listener
                _traceListener = new TextWriterTraceListener(_mainFileName);

                // Setup the debug listener
                Trace.Listeners.Add(_traceListener);
            }

            _initialized = true;

            WriteLine("Application starting");

            // Log the command line
            logCommandLine();
        }

        public static void Dispose()
        {
            WriteLine("Application ended");

            // Flush the trace
            Trace.Flush();

            if (_traceListener != null)
            {
                // Remove the listener
                Trace.Listeners.Remove(_traceListener);

                // Close the listener
                _traceListener.Close();
                _traceListener.Dispose();
                _traceListener = null;
            }

            // Close the trace
            Trace.Close();

            _initialized = false;
        }

        public static void WriteException(string message, Exception exception)
        {
            WriteLine(message);
            WriteLine(exception.ToString());
        }

        public static void WriteException(Exception exception)
        {
            WriteLine(exception.ToString());
        }

        public static void WriteLine(string message)
        {
            if (!_initialized)
                return;

            string sIndent = new string('\t', _indentLevel);

            string sOutput = string.Format(_outputTemplate, DateTime.Now, System.Threading.Thread.CurrentThread.ManagedThreadId, sIndent, message);

            //sOutput = sOutput.Replace("\n", "\\n");
            //sOutput = sOutput.Replace("\r", "\\r");

            if (_echoToConsole)
                Console.WriteLine(sOutput);

            Trace.WriteLine(sOutput);
            Trace.Flush();
        }

        public static void WriteLine(string message, params object[] arguments)
        {
            if (!_initialized)
                return;

            string sMessage = string.Format(message, arguments);

            WriteLine(sMessage);
        }

        public static void WriteLine(string message, IndentOption indentLine)
        {
            if (!_initialized)
                return;

            if (indentLine == IndentOption.DecrementIndentLevel)
                DecrementIndentLevel();

            WriteLine(message);

            if (indentLine == IndentOption.IncrementIndentLevel)
                IncrementIndentLevel();
        }

        public static void IncrementIndentLevel()
        {
            if (!_initialized)
                return;

            _indentLevel++;
        }

        public static void DecrementIndentLevel()
        {
            if (!_initialized)
                return;

            _indentLevel--;
        }

        public static void Flush()
        {
            if (!_initialized)
                return;

            Trace.Flush();
        }

        private static void logCommandLine()
        {
            // Log all command line arguments 
            WriteLine("Command line arguments:");
            int argIndex = 0;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                WriteLine("\tArg {0}: {1}", argIndex++, arg);
            }
        }

        #endregion

        #region Properties

        public static bool Initialized
        {
            get { return _initialized; }
        }

        public static int KeepRevisions
        {
            get { return _keepRevisions; }
            set { _keepRevisions = value; }
        }

        public static string OutputTemplate
        {
            get { return _outputTemplate; }
            set { _outputTemplate = value; }
        }

        public static string FileNameTemplate
        {
            get { return _fileNameTemplate; }
            set { _fileNameTemplate = value; }
        }

        public static TextWriter Writer
        {
            get { return _traceListener.Writer; }
        }

        #endregion
    }
}
