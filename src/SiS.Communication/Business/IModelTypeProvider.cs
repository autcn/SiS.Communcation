using System;

namespace SiS.Communication.Business
{
    internal interface IModelTypeProvider
    {
        Type GetRegisterType(string typeName);
        string GetRegisterName(Type type);
    }
}
