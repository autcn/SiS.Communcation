using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Linq;

namespace SiS.Communication.Spliter
{
    /// <summary>
    /// Represents an object that implements interface of IPacketSpliter, 
    /// which provides methods to split data into packets using end mark
    /// |  Message  | End Mark |
    /// </summary>
    public class EndMarkPacketSpliter : IPacketSpliter
    {
        #region Consturctor
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Spliter.EndMarkPacketSpliter
        /// </summary>
        /// <param name="includeEndMark">true to include end mark in output packet; otherwise, false.</param>
        /// <param name="endMark">The end mark in bytes to split data into packet.</param>
        public EndMarkPacketSpliter(bool includeEndMark, params byte[] endMark)
        {
            _includeEndMark = includeEndMark;
            _endMark = endMark;
        }

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Spliter.EndMarkPacketSpliter
        /// </summary>
        /// <param name="includeEndMark">true to include end mark in output packet; otherwise, false.</param>
        /// <param name="endMarkText">The end mark in string to split data into packet.</param>
        public EndMarkPacketSpliter(bool includeEndMark, string endMarkText)
            : this(includeEndMark, endMarkText, Encoding.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Spliter.EndMarkPacketSpliter
        /// </summary>
        /// <param name="includeEndMark">true to include end mark in output packet; otherwise, false.</param>
        /// <param name="endMarkText">The end mark in string to split data into packet.</param>
        /// <param name="encoding">The end mark text encoding.</param>
        public EndMarkPacketSpliter(bool includeEndMark, string endMarkText, Encoding encoding)
        {
            _includeEndMark = includeEndMark;
            _endMark = encoding.GetBytes(endMarkText);
        }
        #endregion

        #region  Private Members
        private byte[] _endMark;
        private bool _includeEndMark = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether to use MakePacket method. 
        /// </summary>
        /// <returns>true if use MakePacket method; otherwise, false.The default is true.</returns>
        public bool UseMakePacket { get; set; } = true;
        #endregion

        #region Public Functions

        /// <summary>
        /// Get packets from a buffer by finding end mark.
        /// </summary>
        /// <param name="streamBuffer">The source buffer to create packets.</param>
        /// <param name="offset">The starting offset of the buffer to create packets.</param>
        /// <param name="count">The count of the data to create packets.</param>
        /// <param name="endPos">When this method returns, contains the position of the last end mark, if the buffer has 
        /// one complete packet at least, or null if the end mark is not found.</param>
        /// <returns>The packets list if the end mark is found; otherwise, null.</returns>
        public List<ArraySegment<byte>> GetPackets(byte[] streamBuffer, int offset, int count, out int endPos)
        {
            int pos = offset;
            //List<byte[]> packetList = new List<byte[]>();
            List<ArraySegment<byte>> packetList = new List<ArraySegment<byte>>();
            while (true)
            {
                int finishedCount = pos - offset;
                if (finishedCount >= count)
                {
                    break;
                }
                int resultPos = ByteArrayHelper.SearchByteArray(streamBuffer, pos,  count - finishedCount, _endMark);
                if (resultPos == -1)
                {
                    break;
                }
                if (resultPos == pos)
                {
                    pos += _endMark.Length;
                    continue;
                }

                int packetLen = _includeEndMark ? resultPos - pos + _endMark.Length : resultPos - pos;
                //byte[] packet = new byte[packetLen];
                //Array.Copy(streamBuffer, pos, packet, 0, packetLen);
                ArraySegment<byte> packet = new ArraySegment<byte>(streamBuffer, pos, packetLen);
                packetList.Add(packet);
                pos += (resultPos - pos + _endMark.Length);
            }
            endPos = pos;
            if (packetList.Count == 0)
            {
                return null;
            }
            return packetList;
        }

        /// <summary>
        /// Convert a message to a packet using end mark.
        /// </summary>
        /// <param name="messageData">The message data to convert.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to convert.</param>
        /// <param name="sendBuffer">The send buffer which is associated with each connection. It is used to avoid allocating memory every time.</param>
        /// <returns>The packed byte array segment with end mark if UseMakePacket property is true; otherwise the input message data with doing nothing.</returns>
        public ArraySegment<byte> MakePacket(byte[] messageData, int offset, int count, DynamicBuffer sendBuffer)
        {
            Contract.Requires(messageData != null && messageData.Length > 0);
            if (!UseMakePacket)
            {
                return new ArraySegment<byte>(messageData, offset, count);
            }
            lock(sendBuffer)
            {
                sendBuffer.SetLength(count + _endMark.Length);
                //write data
                Array.Copy(messageData, offset, sendBuffer.Buffer, 0, count);
                //write end mark
                Array.Copy(_endMark, 0, sendBuffer.Buffer, count, _endMark.Length);
                return new ArraySegment<byte>(sendBuffer.Buffer, 0, sendBuffer.DataLength);
            }
        }
        #endregion
    }
}
