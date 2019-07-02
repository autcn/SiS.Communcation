namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents a tcp client configuration object.
    /// </summary>
    public class TcpClientConfig : TcpConfig
    {
        /// <summary>
        /// Initializes a new instance of the TcpClientConfig. 
        /// The default SocketAsyncBufferSize is 64K, in bytes.
        /// </summary>
        public TcpClientConfig()
        {
            SocketAsyncBufferSize = 64 * 1024;
        }
    }
}
