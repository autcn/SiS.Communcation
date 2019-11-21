using System;
using System.Net;
using System.Net.Sockets;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Provides common definitions and methods for tcp communication
    /// </summary>
    public static class TcpUtility
    {
        /// <summary>
        /// A 32-bit unsigned integer that represents join group mark.
        /// </summary>
        public const UInt32 JOIN_GROUP_MARK = 0xFA9FCB89;

        /// <summary>
        /// A 32-bit unsigned integer that represents transmiting message to specific group except the sender.
        /// </summary>
        public const UInt32 GROUP_TRANSMIT_MSG_MARK = 0xBCA2BAD4;

        /// <summary>
        /// A 32-bit unsigned integer that represents transmiting message to specific group.
        /// </summary>
        public const UInt32 GROUP_TRANSMIT_MSG_LOOP_BACK_MARK = 0xECA2BAD3;

        /// <summary>
        /// Max group description length in join group messge.
        /// </summary>
        public const int MaxGroupDesLength = 1024;

        /// <summary>
        /// Set socket parameters for keep alive.
        /// </summary>
        /// <param name="socket">The socket to set keep alive parameter.</param>
        /// <param name="keepAliveTime">The keep alive time.</param>
        /// <param name="keepAliveInterval">The keep alive interval.</param>
        public static void SetKeepAlive(Socket socket, uint keepAliveTime, uint keepAliveInterval)
        {
            //the following code is not supported in linux ,so try catch is used here.
            try
            {
                byte[] inValue = new byte[12];
                Array.Copy(BitConverter.GetBytes((int)1), 0, inValue, 0, 4);
                Array.Copy(BitConverter.GetBytes(keepAliveTime), 0, inValue, 4, 4);
                Array.Copy(BitConverter.GetBytes(keepAliveInterval), 0, inValue, 8, 4);
                socket.IOControl(IOControlCode.KeepAliveValues, inValue, null);
            }
            catch
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
        }
    }

    /// <summary>
    /// Provides data for tcp ClientStatusChanged event.
    /// </summary>
    public class ClientStatusChangedEventArgs
    {
        /// <summary>
        /// Gets or sets the client id in long type.
        /// </summary>
        public long ClientID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the source IPEndPoint of the message.
        /// </summary>
        public IPEndPoint IPEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the client status.
        /// </summary>
        /// <returns>The client status. The default is ClientStatus.Closed.</returns>
        public ClientStatus Status { get; set; } = ClientStatus.Closed;
    }
    /// <summary>
    /// Represents the method that will handle the ClientStatusChanged event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">A SiS.Communication.Tcp.ClientStatusChangedEventArgs object that contains the event data.</param>
    public delegate void ClientStatusChangedEventHandler(object sender, ClientStatusChangedEventArgs args);

    /// <summary>
    /// Provides data for tcp MessageReceived event.
    /// </summary>
    public class TcpRawMessageReceivedEventArgs
    {
        /// <summary>
        /// Gets or sets the message that received from the network, see  SiS.Communication.Tcp.TcpMessage.
        /// </summary>
        /// <returns>The message received from the network.</returns>
        public TcpRawMessage Message { get; set; }

        public Exception Error { get; set; }
    }
    /// <summary>
    /// Represents the method that will handle the tcp MessageReceived event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">A SiS.Communication.Tcp.TcpMessageReceivedEventArgs object that contains the event data.</param>
    public delegate void TcpRawMessageReceivedEventHandler(object sender, TcpRawMessageReceivedEventArgs args);
}
