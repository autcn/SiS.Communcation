using System;
using System.Net;
using System.Threading.Tasks;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Represents a client context object.
    /// </summary>
    internal class HttpClientContext
    {
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Tcp.ClientContext
        /// </summary>
        public HttpClientContext()
        {
            ReceiveBuffer = new RingQueue();
            RecvSpeedController = new SpeedController();
            SendController = new SpeedController();
            SendBuffer = new DynamicBufferStream();
        }

        public bool IsWebSocket { get; set; } = false;

        public long ClientID { get; set; }

        /// <summary>
        /// Gets or sets the user data.
        /// </summary>
        public object Tag { get; set; }

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
        public DynamicBufferStream SendBuffer { get; private set; }

        /// <summary>
        /// The speed controller for receiving.
        /// </summary>
        public SpeedController RecvSpeedController { get; set; }

        public BlockStream LargeDataStream { get; set; }

        public HttpContext LargeDataHttpContext { get; set; }

        public Task LargeDataHandleTask { get; set; }

        /// <summary>
        /// The speed controller for sending.
        /// </summary>
        public SpeedController SendController { get; set; }

        public int ContentLength { get; set; } = 0;
        public int RemainLength
        {
            get
            {
                return ContentLength - FinishedLength;
            }
        }
        public int FinishedLength { get; set; } = 0;
        public DateTime LastActiveTime { get; set; }

        internal HttpPacketType PacketType { get; set; } = HttpPacketType.Complete;

        /// <summary>
        /// Reset the object to init state.
        /// </summary>
        public void Reset()
        {
            Tag = null;
           // ClientSocket = null;
           // ClientID = default(long);
            IPEndPoint = null;
            ReceiveBuffer?.Clear();
            SendBuffer?.Clear();
            RecvSpeedController?.Reset();
            SendController?.Reset();
        }
    }
}
