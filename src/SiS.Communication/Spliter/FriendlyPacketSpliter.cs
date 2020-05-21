using System;
using System.Collections.Generic;

namespace SiS.Communication.Spliter
{
    /// <summary>
    /// Represents a class that implements interface of IPacketSpliter, 
    /// which provides simple abstract methods to split data into packets.
    /// The user can derive from this class to make a new class that implemented IPacketSpliter interface easily.
    /// </summary>
    public abstract class FriendlyPacketSpliter : IPacketSpliter
    {
        /// <summary>
        /// Get packets from the buffer
        /// </summary>
        /// <param name="streamBuffer">The source buffer to create packets.</param>
        /// <param name="offset">The starting offset of the buffer to create packets.</param>
        /// <param name="count">The count of the data to create packets.</param>
        /// <param name="clientID">The client id of the data.</param>
        /// <param name="endPos">When this method returns, contains the position of the packet ending, if the buffer has 
        /// one complete packet at least, or null if the packet is not complete.</param>
        /// <returns>The packets list if has complete packet; otherwise, null.</returns>
        public List<DataPacket> GetPackets(byte[] streamBuffer, int offset, int count, long clientID, out int endPos)
        {
            byte[] receivedData = new byte[count];
            Buffer.BlockCopy(streamBuffer, 0, receivedData, 0, count);

            int packetLength = TryGetPacketSize(receivedData);
            if (packetLength <= 0)
            {
                endPos = offset;
                return null;
            }

            DataPacket packet = new DataPacket();
            packet.ClientID = clientID;
            packet.Data = new ArraySegment<byte>(streamBuffer, offset, packetLength);
            endPos = offset + packetLength;
            return new List<DataPacket>() { packet };
        }

        /// <summary>
        /// Convert a message to a packet
        /// </summary>
        /// <param name="messageData">The message data to convert.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to convert.</param>
        /// <param name="sendBuffer">The send buffer which is associated with each connection. It is used to avoid allocating memory every time.</param>
        /// <returns>The packed byte array segment</returns>
        public ArraySegment<byte> MakePacket(byte[] messageData, int offset, int count, DynamicBufferStream sendBuffer)
        {
            byte[] sendData = new byte[count];
            Buffer.BlockCopy(messageData, offset, sendData, 0, count);
            byte[] packetData = MakePacket(sendData);
            return new ArraySegment<byte>(packetData);
        }

        /// <summary>
        /// Try to get the length of the packet from the data buffer
        /// </summary>
        /// <param name="receivedData">The received data from buffer.</param>
        /// <returns>return -1 if the size of the packet is not decided, otherwise the size of the entire packet, include the header and content.</returns>
        public abstract int TryGetPacketSize(byte[] receivedData);

        /// <summary>
        /// Make a sending packet from business data.
        /// </summary>
        /// <param name="toSendData">The business data to be sent</param>
        /// <returns>The entire packet to be sent.</returns>
        public abstract byte[] MakePacket(byte[] toSendData);
    }
}
