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
        /// <summary>
        /// Create an instance of RequestMessageBase
        /// </summary>
        public RequestMessageBase()
        {
            ID = Guid.NewGuid();
        }

        /// <summary>
        /// The request message id.
        /// </summary>
        public Guid ID { get; set; }
    }
}
