using System;

namespace SiS.Communication.Business
{
    internal interface IModelTypeProvider
    {
        Type GetRegisterType(string typeName);
        bool TryGetRegisterType(string typename, out Type type);
        string GetRegisterName(Type type);
        bool TryGetRegisterName(Type type, out string name);
    }
}
