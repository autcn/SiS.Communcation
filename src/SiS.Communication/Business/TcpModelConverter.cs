using System;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Represents a conveter used for tcp mode transmission.
    /// </summary>
    public class TcpModelConverter : ITcpModelConverter
    {
        private ISerializer _serializer;
        private ModelTypeMapper _mapper;
        internal static TcpModelConverter Default { get; private set; }
        private TcpModelConverter(ISerializer serializer)
        {
            _serializer = serializer;
            _mapper = new ModelTypeMapper();
        }
        /// <summary>
        /// Initialize the model converter.
        /// </summary>
        /// <param name="serializer">The serializer to convert data between model and string.</param>
        /// <param name="action">The action for register model types.</param>
        public static void Initialize(ISerializer serializer, Action<IModelTypeRegister> action)
        {
            if (Default == null)
            {
                Default = new TcpModelConverter(serializer);
                action?.Invoke(Default._mapper);
            }
        }

        /// <summary>
        /// Convert model to GeneralMessage.
        /// </summary>
        /// <param name="model">The model to convert.</param>
        /// <returns>The converted result in type of GeneralMessage.</returns>
        public GeneralMessage ToMessage(object model)
        {
            Type modelType = model.GetType();
            string typeName = _mapper.GetRegisterName(modelType);
            GeneralMessage message = new GeneralMessage()
            {
                MessageType = typeName,
                Body = _serializer.Serialize(model)
            };
            return message;
        }

        /// <summary>
        /// Convert GeneralMessage to model
        /// </summary>
        /// <param name="message">An instance in type of GeneralMessage.</param>
        /// <returns>The converted model.</returns>
        public object ToModel(GeneralMessage message)
        {
            Type type = _mapper.GetRegisterType(message.MessageType);
            return _serializer.Deserialize(type, message.Body);
        }

        /// <summary>
        /// Convert message to model in specific type.
        /// </summary>
        /// <typeparam name="T">The model type to convert.</typeparam>
        /// <param name="message">An instance in type of GeneralMessage.</param>
        /// <returns>The converted model in specific type.</returns>
        public T ToModel<T>(GeneralMessage message) where T : ModelMessageBase
        {
            return (T)ToModel(message);
        }
    }
}
