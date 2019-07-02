using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Represents an object used as response basic message.
    /// </summary>
    public abstract class ResponseMessageBase : ModelMessageBase
    {
        /// <summary>
        /// The id of the request message.
        /// </summary>
        public Guid RequestID { get; set; }
    }
}
