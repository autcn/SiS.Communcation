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
        /// Gets or sets a value indicating whether the message can be sent to groups that the client has not joined.
        /// </summary>
        public bool AllowCrossGroupMessage { get; set; } = true;
    }
}
