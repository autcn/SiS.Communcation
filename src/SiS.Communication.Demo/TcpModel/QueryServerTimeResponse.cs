using SiS.Communication.Business;
using System;

namespace SiS.Communication.Demo
{
    public class QueryServerTimeResponse : ResponseMessageBase
    {
        public DateTime ServerTime { get; set; }
    }
}
