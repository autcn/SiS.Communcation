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
