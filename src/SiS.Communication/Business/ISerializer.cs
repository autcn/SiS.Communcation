using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Provide methods to serialize object to string or deserialize string to object.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serialize an model to string.
        /// </summary>
        /// <param name="model">The model to serialize</param>
        /// <returns>The serialize result in string</returns>
        string Serialize(object model);

        /// <summary>
        /// Deserialize string to model with specific type.
        /// </summary>
        /// <param name="type">The type of the model to deserialize</param>
        /// <param name="value">The string to deserialize</param>
        /// <returns>The deserialized model in type.</returns>
        object Deserialize(Type type, string value);
    }
}
