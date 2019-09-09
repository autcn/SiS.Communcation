using SiS.Communication.Business;

namespace SiS.Communication.Demo
{
    public class QueryServerTimeRequest : RequestMessageBase
    {
        public string Message { get; set; }
        public string Name { get; set; }
    }
}
