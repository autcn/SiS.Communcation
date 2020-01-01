using System;
using System.Collections.Generic;

namespace SiS.Communication.Http
{
    internal class WebSocketFrame
    {
        public bool IsEof { get; set; }
        public WSPacketType DataType { get; set; }
        public bool HasMask { get; set; }
        public ArraySegment<byte> MaskData { get; set; }
        public ArraySegment<byte> BodyData { get; set; }
        public ArraySegment<byte> FrameData { get; set; }
    }

    /// <summary>
    /// Represents the web socket packet object.
    /// </summary>
    public class WebSocketPacket
    {
        /// <summary>
        /// Gets or sets the web socket packet type.
        /// </summary>
        public WSPacketType DataType { get; set; }

        /// <summary>
        /// Gets or sets the byte array segment that contains the data of web socket.
        /// </summary>
        public ArraySegment<byte> Data { get; set; }

        internal static List<WebSocketPacket> Convert(List<WebSocketFrame> wsFrames)
        {
            List<WebSocketPacket> packets = new List<WebSocketPacket>();
            WebSocketPacket curPacket = null;
            int startPos = -1;
            int dataLength = 0;
            foreach (WebSocketFrame frame in wsFrames)
            {
                if (curPacket == null)
                {
                    curPacket = new WebSocketPacket();
                    curPacket.DataType = frame.DataType;
                }
                if (startPos == -1)
                {
                    startPos = frame.BodyData.Offset;
                    dataLength = frame.BodyData.Count;
                }
                else
                {
                    Buffer.BlockCopy(frame.BodyData.Array, frame.BodyData.Offset, frame.BodyData.Array, startPos + dataLength,
                        frame.BodyData.Count);
                    dataLength += frame.BodyData.Count;
                }
                if (frame.IsEof)
                {
                    curPacket.Data = new ArraySegment<byte>(frame.BodyData.Array, startPos, dataLength);
                    packets.Add(curPacket);
                    curPacket = null;
                    startPos = -1;
                    dataLength = 0;
                }
            }
            return packets;
        }
    }
    /// <summary>
    /// The sending type of web socket.
    /// </summary>
    public enum WSPacketSendType : byte
    {
        /// <summary>
        /// Indicating the packet data is in type of text.
        /// </summary>
        Text = 1,
        /// <summary>
        /// Indication the packet data is in type oe binary.
        /// </summary>
        Bin = 2
    }
#pragma warning disable 1591
    /// <summary>
    /// The received web socket packet type.
    /// </summary>
    public enum WSPacketType : byte
    {
        Frame = 0,
        Text = 1,
        Bin = 2,
        R3 = 3,
        R4 = 4,
        R5 = 5,
        R6 = 6,
        R7 = 7,
        Disconnect = 8,
        Ping = 9,
        Pang = 0xA,
        R11 = 0xB,
        R12 = 0xC,
        R13 = 0xD,
        R14 = 0xE,
        R15 = 0xF,
    }
#pragma warning restore
}
