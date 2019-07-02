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
    public class ShareMemoryViewModel : PageBaseViewModel
    {
        #region Constructor

        public ShareMemoryViewModel(string title) : base(title)
        {
            _serverVM = new ShareMemoryServerViewModel();
            _clientVM = new ShareMemoryClientViewModel();
            _clientVM.ShowAutoReconnect = false;
            _serverVM.CanSend = false;
            _clientVM.CanReceive = false;
        }
        public ShareMemoryViewModel() : this("Share Memory")
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

    public class ShareMemoryServerViewModel : ServerBaseViewModel
    {
        #region Constructor

        public ShareMemoryServerViewModel()
        {
            _shareMemServer = new ShareMemoryReader("MemTest", 1024 * 1024);
            _shareMemServer.MessageReceived += _shareMemServer_MessageReceived; ;

            ServerSendText = "I am server";
        }

        #endregion

        #region Private Members

        protected ShareMemoryReader _shareMemServer;

        #endregion

        #region Server Event Handlers

        private void _shareMemServer_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _shareMemServer.TextEncoding.GetString(args.Data);
            ServerRecvText += text + "\r\n";
        }

        #endregion

        #region Server Operations

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

    public class ShareMemoryClientViewModel : ClientBaseViewModel
    {
        #region Constructor

        public ShareMemoryClientViewModel()
        {
            _shareMemClient = new ShareMemoryWriter("MemTest", 1024 * 1024);
            ClientSendText = "I am client";
        }

        #endregion

        #region Private Members
        protected ShareMemoryWriter _shareMemClient;
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
