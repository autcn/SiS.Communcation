using SiS.Communication.Spliter;
using SiS.Communication.Tcp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable 1591
namespace SiS.Communication.Business
{
    /// <summary>
    /// A child class derived from TcpClientEx, provides methods to communicate in models simply.
    /// </summary>
    public class TcpModelClient : TcpClientEx
    {
        #region Constructor
        /// <summary>
        /// Create an instance of TcpModelClient
        /// </summary>
        /// <param name="autoReconnect">True if the client can reconnect to server when disconnected automatically; otherwise, false.</param>
        /// <param name="modelHeaderIndicator">A 32-bits integer as the header to indicate the transmitted message is in the type of model.</param>
        public TcpModelClient(bool autoReconnect, int modelHeaderIndicator) : this(autoReconnect, modelHeaderIndicator, new SimplePacketSpliter())
        {
        }

        /// <summary>
        /// Create an instance of TcpModelClient
        /// </summary>
        /// <param name="autoReconnect">True if the client can reconnect to server when disconnected automatically; otherwise, false.</param>
        /// <param name="modelHeaderIndicator">A 32-bits integer as the header to indicate the transmitted message is in the type of model.</param>
        /// <param name="packetSpliter">The spliter which is used to split stream data into packets.</param>
        public TcpModelClient(bool autoReconnect, int modelHeaderIndicator, IPacketSpliter packetSpliter) : base(autoReconnect, packetSpliter)
        {
            _modelHeaderIndicator = modelHeaderIndicator;
            _reqResponseAgent = new RequestResponseAgent(new RelayRequestMessageSender(
            () => RequestTimeOut,
            (clientID, model) => SendModelMessage(model)));
        }
        #endregion

        #region MyRegion
        private int _modelHeaderIndicator;
        private RequestResponseAgent _reqResponseAgent;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the request time out. The defaut is 4000, in millseconds.
        /// </summary>
        public int RequestTimeOut { get; set; } = 4000;

        /// <summary>
        /// Gets the header indicator which represents a model message.
        /// </summary>
        public int HeaderIndicator
        {
            get { return _modelHeaderIndicator; }
        }

        #endregion

        #region Protected Functions
        protected override bool ReceivedMessageFilter(TcpRawMessageReceivedEventArgs tcpRawMessageArgs)
        {
            GeneralMessage serverMessage = GeneralMessage.Deserialize(tcpRawMessageArgs.Message.MessageRawData, false);
            if (serverMessage == null)
            {
                return false;
            }
            //if the server message is model
            if (serverMessage.Header == _modelHeaderIndicator)
            {
                //get model from general message
                object model = TcpModelConverter.Default.ToModel(serverMessage);
                //if model is response message
                if (model is ResponseMessageBase)
                {
                    _reqResponseAgent.SetRequestResult(model as ResponseMessageBase);
                    return false;
                }
            }
            return false;
        }
        #endregion

        #region Public Functions

        /// <summary>
        /// Convert "GeneralMessage" into model.
        /// </summary>
        /// <param name="generalMessage">An instance int the type of GeneralMessage.</param>
        /// <returns>The converted model.</returns>
        public object ConvertToModel(GeneralMessage generalMessage)
        {
            return TcpModelConverter.Default.ToModel(generalMessage);
        }

        /// <summary>
        /// Convert model to "GeneralMessage".
        /// </summary>
        /// <param name="model">The model to convert.</param>
        /// <returns>The instance of GeneralMessage.</returns>
        public GeneralMessage ConvertToGeneralMessage(object model)
        {
            GeneralMessage message = TcpModelConverter.Default.ToMessage(model);
            message.Header = _modelHeaderIndicator;
            return message;
        }

        /// <summary>
        /// Send a model message.
        /// </summary>
        /// <param name="model">The model to be sent.</param>
        public void SendModelMessage(object model)
        {
            GeneralMessage generalMsg = TcpModelConverter.Default.ToMessage(model);
            generalMsg.Header = _modelHeaderIndicator;
            SendMessage(generalMsg.Serialize());
        }

        /// <summary>
        /// Send a model message asynchronously.
        /// </summary>
        /// <param name="model">The model to be sent.</param>
        /// <param name="callback">The callback delegate that will be called when the asynchronous operation finished.</param>
        /// <param name="state">User defined state.</param>
        /// <returns></returns>
        public IAsyncResult SendModelMessageAsync(object model, AsyncCallback callback, object state)
        {
            GeneralMessage generalMsg = TcpModelConverter.Default.ToMessage(model);
            generalMsg.Header = _modelHeaderIndicator;
            return SendMessageAsync(generalMsg.Serialize(), callback, state);
        }

        /// <summary>
        /// Send a model message to a collection of group.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the model message.</param>
        /// <param name="model">The model to be sent.</param>
        /// <param name="loopBack">True if the message returns to the sender; otherwise false. The default is false.</param>
        public void SendGroupModelMessage(IEnumerable<string> groupNameCollection, object model, bool loopBack = false)
        {
            GeneralMessage generalMsg = TcpModelConverter.Default.ToMessage(model);
            generalMsg.Header = _modelHeaderIndicator;
            SendGroupMessage(groupNameCollection, generalMsg.Serialize(), loopBack);
        }

        /// <summary>
        /// Send a model message to a collection of group asynchronously.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the model message.</param>
        /// <param name="model">The model to be sent.</param>
        /// <param name="callback">The callback delegate that will be called when the asynchronous operation finished.</param>
        /// <param name="state">User defined state.</param>
        /// <param name="loopBack">True if the message returns to the sender; otherwise false. The default is false.</param>
        public IAsyncResult SendGroupModelMessageAsync(IEnumerable<string> groupNameCollection, object model, AsyncCallback callback, object state, bool loopBack = false)
        {
            GeneralMessage generalMsg = TcpModelConverter.Default.ToMessage(model);
            generalMsg.Header = _modelHeaderIndicator;
            return SendGroupMessageAsync(groupNameCollection, generalMsg.Serialize(), callback, state, loopBack);
        }

        /// <summary>
        /// Send a request message and get a reponse with default timeout, see "RequestTimeOut" property.
        /// </summary>
        /// <typeparam name="T">The type which is derived from ResponseMessageBase.</typeparam>
        /// <param name="requestMsg">The request to be sent.</param>
        /// <returns>A task object indicating the asynchronous query operation.</returns>
        public Task<T> QueryAsync<T>(RequestMessageBase requestMsg) where T : ResponseMessageBase
        {
            return _reqResponseAgent.QueryAsync<T>(requestMsg);
        }

        /// <summary>
        /// Send a request message and get a reponse with specific timeout.
        /// </summary>
        /// <typeparam name="T">The type which is derived from ResponseMessageBase.</typeparam>
        /// <param name="requestMsg">The request to be sent.</param>
        /// <param name="timeout">The time out of the request, in millseconds.</param>
        /// <returns>A task object indicating the asynchronous query operation.</returns>
        public Task<T> QueryAsync<T>(RequestMessageBase requestMsg, int timeout) where T : ResponseMessageBase
        {
            return _reqResponseAgent.QueryAsync<T>(requestMsg, timeout);
        }
        #endregion
    }

}
