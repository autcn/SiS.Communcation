using System;
using System.Linq;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents an raw tcp message object that received from the network.The data may be stored outside.
    /// </summary>
    public class TcpRawMessage
    {
        /// <summary>
        /// The id of the client where the message from.
        /// </summary>
        public long ClientID { get; set; }

        /// <summary>
        /// Gets or sets the message raw data that received from the network.The raw data storage may use outside buffer.
        /// </summary>
        /// <returns>The message data received from the network.</returns>
        public ArraySegment<byte> MessageRawData { get; set; }

        /// <summary>
        /// Gets or sets the tag of the message.
        /// </summary>
        //public object Tag { get; set; }

        /// <summary>
        /// Convert the raw message to independent message.The data is stored inside the message.
        /// </summary>
        /// <returns>The independent tcp message.</returns>
        public TcpMessage ToTcpMessage()
        {
            TcpMessage tcpMsg = new TcpMessage();
            tcpMsg.ClientID = this.ClientID;
            tcpMsg.MessageData = this.MessageRawData.ToArray();
            return tcpMsg;
        }
    }

    /// <summary>
    /// Repesents an independent tcp message.
    /// </summary>
    public class TcpMessage
    {
        /// <summary>
        /// The id of the client where the message from.
        /// </summary>
        public long ClientID { get; set; }

        /// <summary>
        /// Gets or sets the message data that received from the network.
        /// </summary>
        /// <returns>The message data received from the network.</returns>
        public byte[] MessageData { get; set; }
    }
}
