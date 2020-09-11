using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Linq;

namespace SiS.Communication.Spliter
{
    /// <summary>
    /// Represents an object that implements interface of IPacketSpliter, 
    /// which provides methods to split data into packets using length and specific header
    /// |  Header(4bytes)  | Message Length(4bytes) | Message |
    /// </summary>
    public class HeaderPacketSpliter : IPacketSpliter
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Spliter.HeaderPacketSpliter
        /// </summary>
        /// <param name="useNetworkByteOrder">true to pack length in network byte order; otherwise, int host byte order.</param>
        /// <param name="headerTag">A 32-bit integer as packet header tag to prevent illegal network data.</param>
        public HeaderPacketSpliter(bool useNetworkByteOrder, UInt32 headerTag)
        {
            UseNetworkByteOrder = useNetworkByteOrder;
            _headerTag = headerTag;
        }

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Spliter.HeaderPacketSpliter
        /// </summary>
        /// <param name="headerTag">A 32-bit integer as packet header tag to prevent illegal network data.</param>
        public HeaderPacketSpliter(UInt32 headerTag) : this(false, headerTag) { }

        #endregion

        #region Private Members
        private UInt32 _headerTag;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to use network byte order
        /// </summary>
        /// <returns>true to pack length in network byte order; otherwise, in host byte order.The default is false.</returns>
        public bool UseNetworkByteOrder { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to use MakePacket method. 
        /// </summary>
        /// <returns>true if use MakePacket method; otherwise, false.The default is true.</returns>
        public bool UseMakePacket { get; set; } = true;

        /// <summary>
        /// Gets or sets the max packet length.
        /// </summary>
        /// <returns>The max packet length. The default is 20MB.</returns>
        public int MaxPacketLength { get; set; } = 20 * 1024 * 1024;

        #endregion

        #region Public Functions

        /// <summary>
        /// Convert a message to a packet using packet length and specific header
        /// </summary>
        /// <param name="messageData">The message data to convert.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to convert.</param>
        /// <param name="sendBuffer">The send buffer which is associated with each connection. It is used to avoid allocating memory every time.</param>
        /// <returns>The packed byte array segment with length and header tag if UseMakePacket property is true; otherwise the input message data with doing nothing.</returns>
        public ArraySegment<byte> MakePacket(byte[] messageData, int offset, int count, DynamicBufferStream sendBuffer)
        {
            Contract.Assert(messageData != null && count > 0);
            if (!UseMakePacket)
            {
                return new ArraySegment<byte>(messageData, offset, count);
            }

            int dataLen = count;
            int packetLen = dataLen;
            if (UseNetworkByteOrder)
            {
                packetLen = IPAddress.HostToNetworkOrder(dataLen);
            }

            sendBuffer.SetLength(8 + dataLen);
            //write header
            Buffer.BlockCopy(BitConverter.GetBytes(_headerTag), 0, sendBuffer.Buffer, 0, 4);
            //write message length
            Buffer.BlockCopy(BitConverter.GetBytes(packetLen), 0, sendBuffer.Buffer, 4, 4);
            if (dataLen > 0)
            {
                Buffer.BlockCopy(messageData, offset, sendBuffer.Buffer, 8, dataLen);
            }
            return new ArraySegment<byte>(sendBuffer.Buffer, 0, (int)sendBuffer.Length);
        }

        /// <summary>
        /// Get packets from the buffer using packet length and specific header
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
            int pos = offset;
            List<DataPacket> results = new List<DataPacket>();
            while (true)
            {
                int finishedCount = pos - offset;
                int remainCount = count - finishedCount;
                //the length is not enough for next packet, at least 9bytes.
                if (remainCount <= 8)
                {
                    break;
                }
                //get header
                uint headerTag = BitConverter.ToUInt32(streamBuffer, pos);
                //compare headers to prevent network attack
                if (headerTag != _headerTag)
                {
                    throw new InvalidPacketException();
                }
                //get length
                int contentLen = BitConverter.ToInt32(streamBuffer, pos + 4);
                if (UseNetworkByteOrder)
                {
                    contentLen = IPAddress.NetworkToHostOrder(contentLen);
                }
                if (contentLen <= 0 || contentLen > MaxPacketLength)
                {
                    throw new Exception($"the packet size exceeds the max length:{MaxPacketLength}");
                }
                int requiredLen = 8 + contentLen;
                if (remainCount >= requiredLen)
                {
                    pos += 8;
                    ArraySegment<byte> packet = new ArraySegment<byte>(streamBuffer, pos, contentLen);
                    results.Add(new DataPacket()
                    {
                        Data = packet,
                        ClientID = clientID
                    });
                    pos += contentLen;
                }
                else
                {
                    break;
                }
            }
            endPos = pos;
            if (results.Count == 0)
            {
                return null;
            }
            return results;
        }

        #endregion
    }
}
