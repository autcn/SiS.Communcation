using SiS.Communication.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Demo
{
    public class ServerMessage : ModelMessageBase
    {
        public string Title { get; set; }
        public string Body { get; set; }
    }
}
