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
        /// Get packets from a buffer by finding end mark.
        /// </summary>
        /// <param name="streamBuffer">The source buffer to create packets.</param>
        /// <param name="offset">The starting offset of the buffer to create packets.</param>
        /// <param name="count">The count of the data to create packets.</param>
        /// <param name="endPos">When this method returns, contains the position of the last end mark, if the buffer has 
        /// one complete packet at least, or zero if the end mark is not found.</param>
        /// <returns>The packets list if the end mark is found; otherwise, null.</returns>
        List<ArraySegment<byte>> GetPackets(byte[] streamBuffer, int offset, int count, out int endPos);

        /// <summary>
        /// Convert a message to a packet using end mark.
        /// </summary>
        /// <param name="messageData">The message data to convert.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to convert.</param>
        /// <param name="sendBuffer">The send buffer which is associated with each connection. It is used to avoid allocating memory every time.
        /// A lock must be used when the function is called in multi-threads. Of course, you can choose not to use.</param>
        /// <returns>The packed array segment.</returns>
        ArraySegment<byte> MakePacket(byte[] messageData, int offset, int count, DynamicBuffer sendBuffer);
    }
}
