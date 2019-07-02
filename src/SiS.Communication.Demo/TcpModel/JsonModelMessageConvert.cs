using Newtonsoft.Json;
using SiS.Communication.Business;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace SiS.Communication.Demo
{
    public class JsonModelMessageConvert : ModelMessageConvert
    {
        public static JsonModelMessageConvert Default { private set; get; } = new JsonModelMessageConvert();

        protected override T Deserialize<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        protected override string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        private Dictionary<string, Type> _registeredTypeDict;
        protected override Dictionary<string, Type> RegisteredTypeDict
        {
            get
            {
                if (_registeredTypeDict == null)
                {
                    _registeredTypeDict = new Dictionary<string, Type>();
                    List<Type> children = GetTypeDescendants(Assembly.GetExecutingAssembly(), typeof(ModelMessageBase)).ToList();
                    foreach (Type type in children)
                    {
                        _registeredTypeDict.Add(type.FullName, type);
                    }
                }
                return _registeredTypeDict;
            }
        }
    }
}
