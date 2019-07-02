using SiS.Communication.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Demo
{
    public class QueryServerTimeResponse : ResponseMessageBase
    {
        public DateTime ServerTime { get; set; }
    }
}
