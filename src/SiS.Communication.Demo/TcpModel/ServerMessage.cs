using SiS.Communication.Business;

namespace SiS.Communication.Demo
{
    public class ServerMessage : ModelMessageBase
    {
        public string Title { get; set; }
        public string Body { get; set; }
    }
}
