using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Provides easy methods for request and response operations.
    /// </summary>
    public class RequestResponseAgent
    {
        #region Constructor
        /// <summary>
        /// Create an instance of RequestResponseAgent.
        /// </summary>
        /// <param name="modelMsgSender">The interface for send request message to the server.</param>
        public RequestResponseAgent(IRequestMessageSender modelMsgSender)
        {
            Contract.Requires(modelMsgSender != null);
            _modelSender = modelMsgSender;
            _reqReponseTaskDict = new ConcurrentDictionary<Guid, RequestResponseTask>();
        }
        #endregion

        #region Private Members
        private IRequestMessageSender _modelSender;
        private ConcurrentDictionary<Guid, RequestResponseTask> _reqReponseTaskDict;
        #endregion

        #region Public Functions

        /// <summary>
        /// Sending a request message and get a response.
        /// </summary>
        /// <param name="requestMsg">An instance int the type of RequestMessageBase.</param>
        /// <param name="clientID">The client id is only used when the request message from the server.</param>
        /// <returns></returns>
        public Task<ResponseMessageBase> QueryAsync(RequestMessageBase requestMsg, long clientID = 0)
        {
            return QueryAsync<ResponseMessageBase>(requestMsg, _modelSender.RequestTimeOut);
        }

        /// <summary>
        /// Sending a request message and get a response.
        /// </summary>
        /// <param name="requestMsg">An instance int the type of RequestMessageBase.</param>
        /// <param name="timeout">The request time out in millseconds.</param>
        /// <param name="clientID">The client id is only used when the request message from the server.</param>
        /// <returns></returns>
        public Task<ResponseMessageBase> QueryAsync(RequestMessageBase requestMsg, int timeout, long clientID = 0)
        {
            return QueryAsync<ResponseMessageBase>(requestMsg, timeout);
        }

        /// Sending a request message and get a response.
        /// </summary>
        /// <param name="requestMsg">An instance int the type of RequestMessageBase.</param>
        /// <param name="clientID">The client id is only used when the request message from the server.</param>
        /// <returns></returns>
        public Task<T> QueryAsync<T>(RequestMessageBase requestMsg, long clientID = 0) where T : ResponseMessageBase
        {
            return QueryAsync<T>(requestMsg, _modelSender.RequestTimeOut, clientID);
        }

        /// <summary>
        /// Sending a request message and get a response.
        /// </summary>
        /// <param name="requestMsg">An instance int the type of RequestMessageBase.</param>
        /// <param name="timeout">The request time out in millseconds.</param>
        /// <param name="clientID">The client id is only used when the request message from the server.</param>
        /// <returns></returns>
        public Task<T> QueryAsync<T>(RequestMessageBase requestMsg, int timeout, long clientID = 0) where T : ResponseMessageBase
        {
            return Task.Factory.StartNew<T>(() =>
            {
                RequestResponseTask task = new RequestResponseTask(requestMsg.ID);
                _reqReponseTaskDict.TryAdd(task.TaskID, task);
                try
                {
                    //Send request to server
                    _modelSender.SendRequest(clientID, requestMsg);
                }
                catch (Exception ex)
                {
                    _reqReponseTaskDict.TryRemove(requestMsg.ID, out RequestResponseTask temp1);
                    throw ex;
                }
                //Wait for reponse
                bool isTimeOut = !task.WaitEvent.WaitOne(timeout);
                _reqReponseTaskDict.TryRemove(task.TaskID, out RequestResponseTask temp2);
                task.WaitEvent.Close();
                task.WaitEvent.Dispose();
                if (isTimeOut)
                {
                    throw new Exception("request time out");
                }
                //Get response result
                return (T)task.Result;
            });
        }

        /// <summary>
        /// Set request result with specific message.
        /// </summary>
        /// <param name="responseMsg">An instance in the type of ResponseMessageBase.</param>
        public void SetRequestResult(ResponseMessageBase responseMsg)
        {
            if (_reqReponseTaskDict.TryGetValue(responseMsg.RequestID, out RequestResponseTask task))
            {
                task.Result = responseMsg;
                task.WaitEvent.Set();//notify result
            }
        }

        /// <summary>
        /// Clear all the requests.
        /// </summary>
        public void Clear()
        {
            _reqReponseTaskDict.Clear();
        }
        #endregion


    }

    /// <summary>
    /// Represents an interface for sending request.
    /// </summary>
    public interface IRequestMessageSender
    {
        /// <summary>
        /// Send request message to the server.
        /// </summary>
        /// <param name="clientID">The client's id only used by server.</param>
        /// <param name="request">The request message to be sent.</param>
        void SendRequest(long clientID, RequestMessageBase request);

        /// <summary>
        /// Gets a value indicating the time out during request operation.
        /// </summary>
        int RequestTimeOut { get; }
    }
}
