using SiS.Communication.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SiS.Communication;
using SiS.Communication.Spliter;
using SiS.Communication.Process;

namespace SiS.Communication.Demo
{
    public class ShareMemoryDuplexViewModel : PageBaseViewModel
    {
        #region Constructor

        public ShareMemoryDuplexViewModel(string title) : base(title)
        {
            _serverVM = new ShareMemoryDuplexServerViewModel();
            _clientVM = new ShareMemoryDuplexClientViewModel();
            _clientVM.ShowAutoReconnect = false;
        }
        public ShareMemoryDuplexViewModel() : this("ShareMemoryDuplex")
        { }

        #endregion

        private ServerBaseViewModel _serverVM;
        public override ServerBaseViewModel ServerVM
        {
            get { return _serverVM; }
        }

        private ClientBaseViewModel _clientVM;
        public override ClientBaseViewModel ClientVM
        {
            get { return _clientVM; }
        }
    }

    public class ShareMemoryDuplexServerViewModel : ServerBaseViewModel
    {
        #region Constructor

        public ShareMemoryDuplexServerViewModel()
        {
            _shareMemServer = new ShareMemoryDuplex(true, "MemDuplexTest", 1024 * 1024);
            _shareMemServer.MessageReceived += _shareMemServer_MessageReceived;
            ServerSendText = "I am server";
        }

        #endregion

        #region Private Members

        protected ShareMemoryDuplex _shareMemServer;

        #endregion

        #region Server Event Handlers

        private void _shareMemServer_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _shareMemServer.TextEncoding.GetString(args.Data);
            ServerRecvText += text + "\r\n";
        }

        #endregion

        #region Server Operations

        public override void ServerSend()
        {
            if (!string.IsNullOrWhiteSpace(ServerSendText) && _shareMemServer.IsOpen)
            {
                _shareMemServer.SendText(ServerSendText.Trim());
            }
        }

        public override void StartServer()
        {
            if (CanStartServer)
            {
                _shareMemServer.Open();
                CanStartServer = false;
            }
        }

        public override void StopServer()
        {
            _shareMemServer.Close();
            CanStartServer = true;
        }

        #endregion

    }

    public class ShareMemoryDuplexClientViewModel : ClientBaseViewModel
    {
        #region Constructor

        public ShareMemoryDuplexClientViewModel()
        {
            _shareMemClient = new ShareMemoryDuplex(false, "MemDuplexTest", 1024 * 1024);
            _shareMemClient.MessageReceived += _shareMemClient_MessageReceived;
            ClientSendText = "I am client";
        }

        #endregion

        #region Private Members
        protected ShareMemoryDuplex _shareMemClient;
        #endregion

        #region Client Event Handler
        private void _shareMemClient_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _shareMemClient.TextEncoding.GetString(args.Data);
            ClientRecvText += text + "\r\n";
        }
        #endregion

        #region Client Operations

        public override void ClientSend()
        {
            if (!string.IsNullOrWhiteSpace(ClientSendText) && _shareMemClient.IsOpen)
            {
                _shareMemClient.SendText(ClientSendText.Trim());
            }
        }

        public override void ConnectToServer()
        {
            if (CanConnect)
            {
                CanConnect = false;
                _shareMemClient.Open();
                ClientStatus = ClientStatus.Connected;
            }
        }

        public override void Disconnect()
        {
            _shareMemClient.Close();
            CanConnect = true;
            ClientStatus = ClientStatus.Closed;
        }

        #endregion
    }
}
