using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Represents an object that implements IRequestMessageSender interface.
    /// </summary>
    public class RelayRequestMessageSender : IRequestMessageSender
    {
        #region Contructor
        public RelayRequestMessageSender(Func<int> getTimeOut, Action<long, RequestMessageBase> sendModel)
        {
            _getTimeOut = getTimeOut;
            _sendModel = sendModel;
        }
        #endregion

        #region Private Members
        private Func<int> _getTimeOut;
        private Action<long, RequestMessageBase> _sendModel;
        #endregion

        #region Implements for relay
        public int RequestTimeOut
        {
            get { return _getTimeOut(); }
        }

        public void SendRequest(long clientID, RequestMessageBase requestMessage)
        {
            _sendModel(clientID, requestMessage);
        }
        #endregion


    }
}
