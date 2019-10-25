namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents a tcp server configuration object.
    /// </summary>
    public class TcpServerConfig : TcpConfig
    {
        public static TcpServerConfig Default { get; } = new TcpServerConfig();
        /// <summary>
        /// Initializes a new instance of the TcpServerConfig. 
        /// The default ReceiveDataMaxSpeed is 10M/S, in bytes.
        /// The default SendDataMaxSpeed is 10M/S, in bytes.
        /// </summary>
        public TcpServerConfig()
        {
            ReceiveDataMaxSpeed = 10 * 1024 * 1024;
            SendDataMaxSpeed = 10 * 1024 * 1024;
        }

        /// <summary>
        /// Gets or sets max pending count of listening.
        /// </summary>
        /// <returns>Max pending count of listening. The default is 100.</returns>
        public int MaxPendingCount { get; set; } = 100;

        /// <summary>
        /// Gets or sets the init count of client handlers.
        /// </summary>
        /// <returns>The init count of client handlers. The default is 4.</returns>
        //public int InitHandlerCount { get; set; } = 4;

        /// <summary>
        /// Gets or sets the limited clients count in each handler.
        /// </summary>
        /// <returns>The limited clients count. The default is 10.</returns>
        //public int MaxHandlerClientCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets the max count of clients. The default is 100. 
        /// If the value is bigger than 10000, 10000 will be set as the max count.
        /// </summary>
        public int MaxClientCount { get; set; } = 100;

        /// <summary>
        /// Gets or sets a value indicating whether the message can be sent to groups that the client has not joined.
        /// </summary>
        public bool AllowCrossGroupMessage { get; set; } = true;
    }
}
