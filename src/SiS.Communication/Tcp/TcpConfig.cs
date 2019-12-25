namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents a base tcp parameter object.
    /// </summary>
    public class TcpConfig
    {
        /// <summary>
        /// A const value that indicates the speed is not limited.
        /// </summary>
        public const int NotLimited = -1;
        /// <summary>
        /// Gets or sets a value indicating whether to enable keep alive or not.
        /// </summary>
        /// <returns>true if enable keep alive; otherwise, false; The default is true.</returns>
        public bool EnableKeepAlive { get; set; } = true;

        /// <summary>
        /// Gets or sets keep alive time in Millseconds, the property is only used when EnableKeepAlive is true.
        /// </summary>
        /// <returns>The keep alive time in Millseconds. The default is 3000.</returns>
        public uint KeepAliveTime { get; set; } = 3000;

        /// <summary>
        /// Gets or sets keep alive interval in Millseconds, the property is only used when EnableKeepAlive is true.
        /// </summary>
        /// <returns>The keep alive interval in Millseconds. The default is 3000.</returns>
        public uint KeepAliveInterval { get; set; } = 3000;

        /// <summary>
        /// Gets or sets the asynchronous socket buffer size. The default is 16K, in bytes.
        /// </summary>
        public int SocketAsyncBufferSize { get; set; } = 16 * 1024;

        /// <summary>
        /// Gets or sets the limit speed for receiving, in bytes per second. 
        /// If the value is less or equal to 0,receiving speed control is disabled.The default is -1.
        /// </summary>
        public int ReceiveDataMaxSpeed { get; set; } = NotLimited;

        /// <summary>
        /// Gets or sets the limit speed for sending, in bytes per second. 
        /// If the value is less or equal to 0, sending speed control is disabled.The default is -1.
        /// </summary>
        public int SendDataMaxSpeed { get; set; } = NotLimited;


    }
}
