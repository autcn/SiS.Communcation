using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Represents an object used as request basic message.
    /// </summary>
    public abstract class RequestMessageBase : ModelMessageBase
    {
        public RequestMessageBase()
        {
            ID = new Guid();
        }

        /// <summary>
        /// The request message id.
        /// </summary>
        public Guid ID { get; set; }
    }
}
