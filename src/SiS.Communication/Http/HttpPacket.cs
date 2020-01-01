using System;

namespace SiS.Communication.Http
{
    internal enum HttpPacketType
    {
        Complete,
        Begin,
        Bin,
        End
    }
    internal class HttpPacket : DataPacket
    {
        public HttpPacketType PacketType { get; set; }
        public ArraySegment<byte>? HeaderData { get; set; }
        public ArraySegment<byte>? BodyData { get; set; }
        public int BodyTotalLength { get; set; } = 0;
    }
}
