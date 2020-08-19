using SiS.Communication.Common;
using System;
using System.Reflection;

namespace SiS.Communication.Business
{
    internal class ModelTypeMapper : IModelTypeRegister, IModelTypeProvider
    {
        private BiMap<string, Type> _biMap;
        public ModelTypeMapper()
        {
            _biMap = new BiMap<string, Type>();
        }

        public string GetRegisterName(Type type)
        {
            return _biMap.GetKey(type);
        }

        public Type GetRegisterType(string typeName)
        {
            return _biMap[typeName];
        }

        public void Register(string typeName, Type type)
        {
            _biMap[typeName] = type;
        }

        public void Register(Type type)
        {
            Register(type.FullName, type);
        }

        public void Register<T>(string typeName) where T : ModelMessageBase
        {
            Register(typeName, typeof(T));
        }

        public void Register<T>() where T : ModelMessageBase
        {
            Register(typeof(T));
        }

        public void Register(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            Type baseType = typeof(ModelMessageBase);
            foreach (Type type in types)
            {
                if (!type.IsAbstract && type.IsSubclassOf(baseType))
                {
                    Register(type);
                }
            }
        }
    }
}
