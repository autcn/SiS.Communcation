using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Provides convert methods between GeneralMessage and model.
    /// </summary>
    public abstract class ModelMessageConvert
    {
        #region Constructor
        public ModelMessageConvert()
        {
            Type thisType = typeof(ModelMessageConvert);
            _deserializeMethod = thisType.GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Instance);

        }
        #endregion

        #region Private Members
        private MethodInfo _deserializeMethod;
        #endregion

        #region Abstract Functions

        /// <summary>
        /// Serialize model into string. The type of the model must be registered in dict. See RegisteredTypeDict.
        /// </summary>
        /// <param name="model">The model to serialize.</param>
        /// <returns>The serialize result in string type.</returns>
        protected abstract string Serialize(object model);

        /// <summary>
        /// Deserialize text into model.The type of T must be registered in dict. See RegisteredTypeDict.
        /// </summary>
        /// <param name="text">The text to deserialize.</param>
        /// <returns>The deserialized model.</returns>
        protected abstract T Deserialize<T>(string text);

        /// <summary>
        /// A dictionary representing the mapping between strings and type, which is used to serialize model and deserialize text.
        /// </summary>
        protected abstract Dictionary<string, Type> RegisteredTypeDict { get; }

        #endregion

        #region Public Functions

        /// <summary>
        /// Convert "GeneralMessage" into model.If the "MessageType" is not found in the mapping dictionary, an exception will be thrown out.
        /// </summary>
        /// <param name="message">The message to convert, see GeneralMessage.</param>
        /// <returns>The converted model.</returns>
        public object ToModel(GeneralMessage message)
        {
            if (!RegisteredTypeDict.ContainsKey(message.MessageType))
            {
                throw new MessageTypeNotRegisteredException($"{message.MessageType} is not registered.");
            }
            Type type = RegisteredTypeDict[message.MessageType];
            MethodInfo deMethod = _deserializeMethod.MakeGenericMethod(type);
            return deMethod.Invoke(this, new object[] { message.Body });
        }

        /// <summary>
        /// Convert model into "GeneralMessage".If the type of the model is not found in the mapping dictionary, an exception will be thrown out.
        /// </summary>
        /// <param name="model">The model to convert.</param>
        /// <returns>The converted message, see GeneralMessage.</returns>
        public GeneralMessage ToMessage(object model)
        {
            string messageTypeName = null;
            Type modelType = model.GetType();
            foreach (string typeName in RegisteredTypeDict.Keys)
            {
                if (RegisteredTypeDict[typeName].Equals(modelType))
                {
                    messageTypeName = typeName;
                    break;
                }
            }
            if (messageTypeName == null)
            {
                throw new Exception("the model is not registered.");
            }
            GeneralMessage message = new GeneralMessage()
            {
                MessageType = messageTypeName,
                Body = this.Serialize(model)
            };
            return message;
        }

        #endregion

        #region Static Functions
        /// <summary>
        /// Get the descendants of the types in specific assembly.
        /// </summary>
        /// <param name="assembly">The assembly to search.</param>
        /// <param name="baseTypes">The types to get descendants.</param>
        /// <returns>The descendants of the input types.</returns>
        public static IEnumerable<Type> GetTypeDescendants(Assembly assembly, params Type[] baseTypes)
        {
            List<Type> typeList = new List<Type>();
            foreach (Type baseType in baseTypes)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.Attributes.HasFlag(TypeAttributes.Abstract))
                    {
                        continue;
                    }
                    Type tempType = type;
                    while (tempType.BaseType != null)
                    {
                        if (tempType.BaseType.Equals(baseType))
                        {
                            typeList.Add(type);
                            break;
                        }
                        else
                        {
                            tempType = tempType.BaseType;
                        }
                    }
                }
            }
            return typeList;
        }

        /// <summary>
        /// Get the descendants of the type in hosted assembly.
        /// </summary>
        /// <param name="baseTypes">The type to get descendants.</param>
        /// <returns>The descendants of the input type.</returns>
        public static IEnumerable<Type> GetTypeDescendants(Type baseType)
        {
            return GetTypeDescendants(baseType.Assembly, baseType);
        }
        #endregion
    }
}
