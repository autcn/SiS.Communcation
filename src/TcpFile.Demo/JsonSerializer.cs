using Newtonsoft.Json;
using SiS.Communication.Business;
using System;

namespace TcpFile.Demo
{
    public class JsonSerializer : ISerializer
    {
        public static JsonSerializer Default { get; } = new JsonSerializer();
        public object Deserialize(Type type, string value)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        public string Serialize(object model)
        {
            return JsonConvert.SerializeObject(model);
        }
    }
}
