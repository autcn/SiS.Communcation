using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiS.Communication.Business
{
    internal interface ITcpModelConverter
    {
        object ToModel(GeneralMessage message);
        T ToModel<T>(GeneralMessage message) where T : ModelMessageBase;
        GeneralMessage ToMessage(object model);
    }
}
