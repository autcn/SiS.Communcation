using System.Net;

namespace SiS.Communication.Udp
{
    /// <summary>
    /// Represents an udp message object that received from the network.
    /// </summary>
    public class UdpMessage
    {
        /// <summary>
        /// Gets or sets the source IPEndPoint of the message.
        /// </summary>
        /// <returns>The source IPEndPoint of the message.</returns>
        public IPEndPoint IPEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the message data that received from the network.
        /// </summary>
        /// <returns>The message data received from the network.</returns>
        public byte[] MessageData { get; set; }
    }
}
