using System;
using System.Collections.Generic;

namespace SiS.Communication.Spliter
{
    /// <summary>
    /// Provide methods to split stream data into packets or convert message data into packet for sending.
    /// </summary>
    public interface IPacketSpliter
    {
        /// <summary>
        /// Get packets from a buffer
        /// </summary>
        /// <param name="streamBuffer">The source buffer to create packets.</param>
        /// <param name="offset">The starting offset of the buffer to create packets.</param>
        /// <param name="count">The count of the data to create packets.</param>
        /// <param name="clientID">The client id of the data.</param>
        /// <param name="endPos">When this method returns, contains the position of the packet ending if the buffer has 
        /// one complete packet at least, or zero if the end mark is not found.</param>
        /// <returns>The packets list if the buffer has complete packet; otherwise, null.</returns>
        List<DataPacket> GetPackets(byte[] streamBuffer, int offset, int count, long clientID, out int endPos);

        /// <summary>
        /// Convert a message to a packet using end mark.
        /// </summary>
        /// <param name="messageData">The message data to convert.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to convert.</param>
        /// <param name="sendBuffer">The send buffer which is associated with each connection. It is used to avoid allocating memory every time.</param>
        /// <returns>The packed array segment.</returns>
        ArraySegment<byte> MakePacket(byte[] messageData, int offset, int count, DynamicBufferStream sendBuffer);
    }
}
