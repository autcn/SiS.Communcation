using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents a client context object.
    /// </summary>
    public class ClientContext
    {
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Tcp.ClientContext
        /// </summary>
        public ClientContext()
        {
            ReceiveBuffer = new RingQueue();
            RecvSpeedController = new SpeedController();
            SendController = new SpeedController();
            RecvRawMessage = new TcpRawMessage();
            SendBuffer = new DynamicBuffer();
        }

        /// <summary>
        /// Gets or sets the user data.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Gets or sets the id of the hander that contains the client.
        /// </summary>
        //public Guid HandlerID { get; set; }

        /// <summary>
        /// Gets or sets the basic Socket.
        /// </summary>
        public Socket ClientSocket { get; set; }

        /// <summary>
        /// Gets or sets the client id in long type.
        /// </summary>
        public long ClientID { get; set; }

        /// <summary>
        /// Gets or sets the IPEndPoint of the client.
        /// </summary>
        public IPEndPoint IPEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the receive buffer.
        /// </summary>
        public RingQueue ReceiveBuffer { get; private set; }

        /// <summary>
        /// Gets or sets the send buffer.
        /// </summary>
        public DynamicBuffer SendBuffer { get; private set; }

        /// <summary>
        /// Gets or sets the groups where the client is located.
        /// </summary>
        public HashSet<string> Groups { get; set; }

        /// <summary>
        /// Gets or sets the SocketAsyncEventArgs
        /// </summary>
        public SocketAsyncEventArgs SockAsyncArgs { get; set; }

        /// <summary>
        /// Gets or sets the client status.
        /// </summary>
        /// <returns>The client status. The default is ClientStatus.Closed.</returns>
        public ClientStatus Status { get; set; } = ClientStatus.Closed;

        /// <summary>
        /// Gets or sets the TcpRawMessage object to store received tcp data.
        /// </summary>
        public TcpRawMessage RecvRawMessage { get; set; }

        /// <summary>
        /// The speed controller for receiving.
        /// </summary>
        public SpeedController RecvSpeedController { get; set; }

        /// <summary>
        /// The speed controller for sending.
        /// </summary>
        public SpeedController SendController { get; set; }

        /// <summary>
        /// Reset the object to init state.
        /// </summary>
        public void Reset()
        {
            Tag = null;
            ClientSocket = null;
            ClientID = default(long);
            IPEndPoint = null;
            ReceiveBuffer?.Clear();
            SendBuffer?.Clear();
            Groups = null;
            Status = ClientStatus.Closed;
            RecvSpeedController?.Reset();
            SendController?.Reset();
            RecvRawMessage.ClientID = default(long);
            RecvRawMessage.MessageRawData = default(ArraySegment<byte>);
            if (SockAsyncArgs != null)
            {
                SockAsyncArgs.AcceptSocket = null;
                SockAsyncArgs.UserToken = null;
            }
        }
    }
}
