using System;
using System.IO;

namespace SiS.Communication
{
    /// <summary>
    /// Specific the output mode for the SiS.Communication.LogWriter class
    /// </summary>
    [Flags]
    public enum LogOutMode
    {
        File = 1,
        Console = 2,
        Trace = 4
    }

    /// <summary>
    /// Provides methods for output log
    /// </summary>
    public class LogWriter : LogBase
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.LogWriter.
        /// </summary>
        /// <param name="directory">The directory to save the log files.</param>
        /// <param name="outMode">The output mode of the log.</param>
        public LogWriter(string directory, LogOutMode outMode)
        {
            StorageDirectory = directory;
            _lockObj = new object();
            OutputLevel = LogLevel.Error | LogLevel.Fatal;
            OutputMode = outMode;
        }

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.LogWriter. The default output mode is combined with File and Trace.
        /// </summary>
        /// <param name="directory">The directory to save the log files.</param>
        public LogWriter(string directory) : this(directory, LogOutMode.File | LogOutMode.Trace)
        {
        }
        #endregion

        #region Private Members
        private object _lockObj;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the directory to save log.
        /// </summary>
        public string StorageDirectory { get; private set; }

        /// <summary>
        /// Gets or sets the mode to output log.
        /// </summary>
        /// <returns>The mode to output log. The default is LogOutMode.File.</returns>
        public LogOutMode OutputMode { get; set; }

        /// <summary>
        /// Gets or sets the output level of the log.
        /// </summary>
        public LogLevel OutputLevel { get; set; }
        #endregion

        #region Public Functions

        /// <summary>
        /// Write a log.
        /// </summary>
        /// <param name="level">The level of the log.</param>
        /// <param name="message">The log message to output.</param>
        /// <param name="reason">The reason to output.</param>
        public override void WriteLog(LogLevel level, string message, string reason)
        {
            if (OutputLevel.HasFlag(LogLevel.Off))
            {
                return;
            }
            if (!OutputLevel.HasFlag(LogLevel.All))
            {
                if (!OutputLevel.HasFlag(level))
                {
                    return;
                }
            }
            try
            {
                lock (_lockObj)
                {
                    string strDateTime = DateTime.Now.ToString("u");
                    strDateTime = strDateTime.TrimEnd('Z');
                    string strMsg = "";
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        strMsg = $"{strDateTime} [{level.ToString()}] {message} >>> Reason: {reason}";
                    }
                    else
                    {
                        strMsg = $"{strDateTime} [{level.ToString()}] {message}";
                    }


                    if (OutputMode.HasFlag(LogOutMode.File))
                    {
                        if (!Directory.Exists(StorageDirectory))
                        {
                            Directory.CreateDirectory(StorageDirectory);
                        }
                        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        fileName = Path.Combine(StorageDirectory, fileName);
                        File.AppendAllText(fileName, strMsg + "\r\n");
                    }
                    if (OutputMode.HasFlag(LogOutMode.Console))
                    {
                        Console.WriteLine(strMsg);
                    }
                    if (OutputMode.HasFlag(LogOutMode.Trace))
                    {
                        System.Diagnostics.Trace.WriteLine(strMsg);
                    }
                }
            }
            catch
            {

            }
        }
        #endregion

    }
}
