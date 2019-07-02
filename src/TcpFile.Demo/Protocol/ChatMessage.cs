using SiS.Communication.Business;

namespace TcpFile.Demo.Protocol
{
    public class ChatMessage : ModelMessageBase
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public double FontSize { get; set; }
        public string FontFamily { get; set; }
        public byte[] Color { get; set; }
    }
}
