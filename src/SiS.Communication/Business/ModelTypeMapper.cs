using SiS.Communication.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool TryGetRegisterName(Type type, out string name)
        {
            if (!_biMap.ContainsValue(type))
            {
                name = null;
                return false;
            }
            name = GetRegisterName(type);
            return true;
        }

        public bool TryGetRegisterType(string typename, out Type type)
        {
            if (!_biMap.ContainsKey(typename))
            {
                type = null;
                return false;
            }
            type = GetRegisterType(typename);
            return true;
        }

        public void Register(string typeName, Type type)
        {
            _biMap[typeName] = type;
        }

        public void Register(Type type)
        {
            Register(type.FullName, type);
        }

        public void Register(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                Register(type);
            }
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
                if (!type.IsAbstract && !type.IsGenericType && type.IsSubclassOf(baseType))
                {
                    Register(type);
                }
            }
        }

        public void RegisterGeneric(Type genericType, params Type[] dataTypes)
        {
            foreach (Type dataType in dataTypes)
            {
                if (dataType.IsGenericType)
                {
                    throw new Exception($"The data type: {dataType.Name} can not be generic");
                }
                if (dataType.IsAbstract)
                {
                    throw new Exception($"The data type: {dataType.Name} can not be abstract");
                }
                Type newType = genericType.MakeGenericType(dataType);
                Register(newType);
            }
        }

        public void RegisterGeneric(Type genericType, IEnumerable<Type> dataTypes)
        {
            RegisterGeneric(genericType, dataTypes.ToArray());
        }


    }
}
