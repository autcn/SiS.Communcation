using System;
#pragma warning disable 1591
namespace SiS.Communication
{
    /// <summary>
    /// The exception that is thrown when calling a starting function on already running task.
    /// </summary>
    public class AlreadyRunningException : Exception
    {
        public AlreadyRunningException(string message) : base(message)
        {

        }
        public AlreadyRunningException()
        {

        }
    }

    /// <summary>
    /// The exception that is thrown when doing some opertions on not running task.
    /// </summary>
    public class NotRunningException : Exception
    {
        public NotRunningException(string message) : base(message)
        {

        }
        public NotRunningException()
        {

        }
    }

    /// <summary>
    /// The exception that is thrown when received an invalid packet
    /// </summary>
    public class InvalidPacketException : Exception
    {
        public InvalidPacketException(string message) : base(message)
        {

        }
        public InvalidPacketException()
        {

        }
    }

    /// <summary>
    /// The exception that is thrown when the queue is full
    /// </summary>
    public class QueueFullException : Exception
    {
        public QueueFullException(string message) : base(message)
        {

        }
        public QueueFullException() { }
    }

    /// <summary>
    /// The exception that is thrown when the message type is not registered.
    /// </summary>
    public class MessageTypeNotRegisteredException : Exception
    {
        public MessageTypeNotRegisteredException(string message) : base(message)
        {

        }
        public MessageTypeNotRegisteredException() { }
    }
}
