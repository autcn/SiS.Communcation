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
    public class PipeViewModel : PageBaseViewModel
    {
        #region Constructor

        public PipeViewModel(string title) : base(title)
        {
            _serverVM = new PipeServerViewModel();
            _clientVM = new PipeClientViewModel();
            _clientVM.ShowAutoReconnect = false;
        }
        public PipeViewModel() : this("Pipe")
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

    public class PipeServerViewModel : ServerBaseViewModel
    {
        #region Constructor

        public PipeServerViewModel()
        {
            _pipeServer = new PipeServer();
            _pipeServer.ClientStatusChanged += _pipeServer_ClientStatusChanged;
            _pipeServer.MessageReceived += _pipeServer_MessageReceived; ;

            ServerSendText = "I am server";
        }

        #endregion

        #region Private Members

        protected PipeServer _pipeServer;

        #endregion

        #region Server Event Handlers

        private void _pipeServer_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _pipeServer.TextEncoding.GetString(args.Data);
            ServerRecvText += text + "\r\n";
        }

        private void _pipeServer_ClientStatusChanged(object sender, PipeClientStatusChangedEventArgs args)
        {

        }

        #endregion

        #region Server Operations

        public override void ServerSend()
        {
            if (_pipeServer.Status == ClientStatus.Connected && !string.IsNullOrWhiteSpace(ServerSendText))
            {
                byte[] messageData = _pipeServer.TextEncoding.GetBytes(ServerSendText.Trim());
                _pipeServer.SendMessage(messageData);
            }
        }

        public override void StartServer()
        {
            if (CanStartServer)
            {
                _pipeServer.Start("PipeNameXXX");
                CanStartServer = false;
            }
        }

        public override void StopServer()
        {
            _pipeServer.Stop();
            CanStartServer = true;
        }

        #endregion

    }

    public class PipeClientViewModel : ClientBaseViewModel
    {
        #region Constructor

        public PipeClientViewModel()
        {
            _pipeClient = new PipeClient();
            _pipeClient.ClientStatusChanged += _pipeClient_ClientStatusChanged;
            _pipeClient.MessageReceived += _pipeClient_MessageReceived;

            ClientSendText = "I am client";
        }

        #endregion

        #region Private Members
        protected PipeClient _pipeClient;
        private const ushort _serverPort = 9999;
        #endregion

        #region Client Event Handlers

        private void _pipeClient_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _pipeClient.TextEncoding.GetString(args.Data);
            ClientRecvText += text + "\r\n";
        }

        private void _pipeClient_ClientStatusChanged(object sender, PipeClientStatusChangedEventArgs args)
        {
            ClientStatus = args.Status;
            if (args.Status == ClientStatus.Closed)
            {
                CanConnect = true;
            }
        }

        #endregion

        #region Client Operations

        public override void ClientSend()
        {
            if (!string.IsNullOrWhiteSpace(ClientSendText) && _pipeClient.Status == ClientStatus.Connected)
            {
                _pipeClient.SendText(ClientSendText.Trim());
            }
        }

        public override void ConnectToServer()
        {
            if (CanConnect)
            {
                CanConnect = false;
                _pipeClient.Connect("PipeNameXXX");
            }
        }

        public override void Disconnect()
        {
            _pipeClient.Close();
        }

        #endregion
    }
}
