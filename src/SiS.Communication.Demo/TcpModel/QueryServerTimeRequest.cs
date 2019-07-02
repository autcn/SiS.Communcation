using SiS.Communication.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Demo
{
    public class QueryServerTimeRequest : RequestMessageBase
    {
        public string Message { get; set; }
        public string Name { get; set; }
    }
}
