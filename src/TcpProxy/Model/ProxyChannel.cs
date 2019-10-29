using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpProxy.Model
{
    public class ProxyChannel
    {
        public string Name { get; set; }

        public int? ListenPort { get; set; }

        public string RemoteIP { get; set; }

        public int? RemotePort { get; set; }
    }
}
