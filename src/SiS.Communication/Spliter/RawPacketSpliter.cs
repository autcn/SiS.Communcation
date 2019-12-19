using System;
using System.Collections.Generic;
using System.Linq;

namespace SiS.Communication.Spliter
{
    /// <summary>
    /// Represents an object that implements interface of IPacketSpliter, 
    /// which transmit all data with doing nothing.
    /// |  Raw Data Stream |
    /// </summary>
    public class RawPacketSpliter : IPacketSpliter
    {
        /// <summary>
        /// The static default instance of SiS.Communication.Spliter.RawPacketSpliter
        /// </summary>
        public static RawPacketSpliter Default { get; private set; } = new RawPacketSpliter();

        #region Public Functions

        /// <summary>
        /// Get packets from a buffer directly.
        /// </summary>
        /// <param name="streamBuffer">The source buffer to create packets.</param>
        /// <param name="offset">The starting offset of the buffer to create packets.</param>
        /// <param name="count">The count of the data to create packets.</param>
        /// <param name="clientID">The client id of the data.</param>
        /// <param name="endPos">When this method returns, contains the position of the packet ending if the buffer has 
        /// one complete packet at least, or zero if the end mark is not found.</param>
        /// <returns>The packets list that contains input buffer.</returns>
        public List<DataPacket> GetPackets(byte[] streamBuffer, int offset, int count, long clientID, out int endPos)
        {
            endPos = Math.Min(streamBuffer.Length, offset + count);
            return new List<DataPacket>()
            {
                new DataPacket()
                {
                     Data = new ArraySegment<byte>(streamBuffer, offset, count),
                     ClientID = clientID
                }
            };
        }

        /// <summary>
        /// Make a message into packet with doing nothing.
        /// </summary>
        /// <param name="messageData">The message data to convert.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to convert.</param>
        /// <param name="sendBuffer">The send buffer which is associated with each connection. It is not used is this spliter.</param>
        /// <returns>The same packed byte array segment as input.</returns>
        public ArraySegment<byte> MakePacket(byte[] messageData, int offset, int count, DynamicBuffer sendBuffer)
        {
            return new ArraySegment<byte>(messageData, offset, count);
        }

        #endregion
    }
}
