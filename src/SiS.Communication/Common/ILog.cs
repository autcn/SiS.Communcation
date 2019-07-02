using System;

namespace SiS.Communication
{
    [Flags]
    public enum LogLevel : int
    {
        All = 0,
        Debug = 1,
        Info = 2,
        Warn = 4,
        Error = 8,
        Fatal = 16,
        Off = 32
    }

    public interface ILog
    {
        void WriteLog(LogLevel level, string message, string reason);
        void WriteLog(LogLevel level, string message);
        void Debug(string message, string reason);
        void Debug(string message);
        void Info(string message, string reason);
        void Info(string message);
        void Warn(string message, string reason);
        void Warn(string message);
        void Error(string message, string reason);
        void Error(string message);
        void Fatal(string message, string reason);
        void Fatal(string message);
    }

    public abstract class LogBase : ILog
    {
        public abstract void WriteLog(LogLevel level, string message, string reason);

        public void WriteLog(LogLevel level, string message)
        {
            WriteLog(level, message, null);
        }

        public void Debug(string message, string reason)
        {
            WriteLog(LogLevel.Debug, message, reason);
        }

        public void Debug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        public void Info(string message, string reason)
        {
            WriteLog(LogLevel.Info, message, reason);
        }

        public void Info(string message)
        {
            WriteLog(LogLevel.Info, message);
        }

        public void Warn(string message, string reason)
        {
            WriteLog(LogLevel.Warn, message, reason);
        }

        public void Warn(string message)
        {
            WriteLog(LogLevel.Warn, message);
        }

        public void Error(string message, string reason)
        {
            WriteLog(LogLevel.Error, message, reason);
        }

        public void Error(string message)
        {
            WriteLog(LogLevel.Error, message);
        }

        public void Fatal(string message, string reason)
        {
            WriteLog(LogLevel.Fatal, message, reason);
        }

        public void Fatal(string message)
        {
            WriteLog(LogLevel.Fatal, message);
        }
    }
}

