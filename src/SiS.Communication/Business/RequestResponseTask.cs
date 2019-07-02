using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiS.Communication.Business
{
    internal class RequestResponseTask
    {
        public RequestResponseTask(Guid id)
        {
            WaitEvent = new ManualResetEvent(false);
            TaskID = id;
        }

        public RequestResponseTask() : this(Guid.NewGuid())
        {
        }

        public Guid TaskID { get; private set; }
        public object Result { get; set; }
        public ManualResetEvent WaitEvent { get; }
    }
}
