using SiS.Communication.Tcp;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Represents the configuration of a http server.
    /// </summary>
    public class HttpServerConfig
    {
        /// <summary>
        /// Create an instance of http server configuration.
        /// </summary>
        public HttpServerConfig()
        {
            TcpConfig = new TcpServerConfig();
            TcpConfig.ReceiveDataMaxSpeed = TcpServerConfig.NotLimited;
            TcpConfig.SendDataMaxSpeed = TcpServerConfig.NotLimited;
            TcpConfig.EnableKeepAlive = false;
        }

        /// <summary>
        /// The max header length of a http request header.The default value is 20KB
        /// </summary>
        public int MaxHeaderLength { get; set; } = 20 * 1024;

        /// <summary>
        /// The max size of body cache.If the body length less than the value, a byte array will be returned.
        /// If the body length exceeds the value, a block stream will be returned.
        /// The default value is 4MB.
        /// </summary>
        public int MaxBodyCache { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// The max size of the web socket packet.The defaul value is 6MB.
        /// </summary>
        public long WSMaxPacketLength { get; set; } = 6 * 1024 * 1024;

        /// <summary>
        /// The max length of the request url. The default value is 5K.
        /// </summary>
        public int MaxUrlLength { get; set; } = 5 * 1024;

        /// <summary>
        /// The timeout in mill seconds during request. The default value is 5000ms.
        /// </summary>
        public int RequestTimeout { get; set; } = 5 * 1000;

        /// <summary>
        /// The tcp configuration of the http server.
        /// </summary>
        public TcpServerConfig TcpConfig { get; set; }
    }
}
