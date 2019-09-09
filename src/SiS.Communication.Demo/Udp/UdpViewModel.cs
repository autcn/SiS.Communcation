using SiS.Communication.Udp;
using System.Net;

namespace SiS.Communication.Demo
{
    public class UdpViewModel : PageBaseViewModel
    {
        #region Constructor

        public UdpViewModel(string title) : base(title)
        {
            _serverVM = new UdpServerViewModel();
            _clientVM = new UdpClientViewModel();
        }
        public UdpViewModel() : this("Udp")
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

    public class UdpServerViewModel : ServerBaseViewModel
    {
        #region Constructor

        public UdpServerViewModel()
        {
            _udpServer = new UdpServer();
            _udpServer.MessageReceived += _udpServer_MessageReceived;
            ServerSendText = "I am server";
        }

        #endregion

        #region Private Members

        protected UdpServer _udpServer;

        #endregion

        #region Properties
        private bool _useMulticast = true;
        public bool UseMulticast
        {
            get { return _useMulticast; }
            set
            {
                if (value != _useMulticast)
                {
                    _useMulticast = value;
                    NotifyPropertyChanged(nameof(UseMulticast));
                }
            }
        }

        #endregion

        #region Server Event Handlers

        private void _udpServer_MessageReceived(object sender, UdpMessageReceivedEventArgs args)
        {
            string text = _udpServer.TextEncoding.GetString(args.Message.MessageData);
            ServerRecvText += text + "\r\n";
        }

        #endregion

        #region Server Operations

        public void SendGroupMessage()
        {
            if (_useMulticast && _udpServer.IsRunning)
            {
                byte[] messageData = _udpServer.TextEncoding.GetBytes(ServerSendText.Trim());
                _udpServer.SendGroupMessage(messageData, 9002);
            }
        }

        public override void ServerSend()
        {
            if (!string.IsNullOrWhiteSpace(ServerSendText) && _udpServer.IsRunning)
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9002);
                _udpServer.SendText(ipEndPoint, ServerSendText.Trim());
            }
        }

        public override void StartServer()
        {
            if (CanStartServer)
            {
                if (_useMulticast)
                {
                    _udpServer.Start("224.1.1.1", 9001);
                }
                else
                {
                    _udpServer.Start(9001);
                }

                CanStartServer = false;
            }
        }

        public override void StopServer()
        {
            _udpServer.Stop();
            CanStartServer = true;
        }

        #endregion

    }

    public class UdpClientViewModel : ClientBaseViewModel
    {
        #region Constructor

        public UdpClientViewModel()
        {
            _udpClient = new UdpServer();
            _udpClient.MessageReceived += _udpClient_MessageReceived;
            ClientSendText = "I am client";
        }



        #endregion

        #region Properties
        private bool _useMulticast = true;
        public bool UseMulticast
        {
            get { return _useMulticast; }
            set
            {
                if (value != _useMulticast)
                {
                    _useMulticast = value;
                    NotifyPropertyChanged(nameof(UseMulticast));
                }
            }
        }

        #endregion

        #region Private Members
        protected UdpServer _udpClient;
        #endregion

        #region Client Event Handlers
        private void _udpClient_MessageReceived(object sender, UdpMessageReceivedEventArgs args)
        {
            string text = _udpClient.TextEncoding.GetString(args.Message.MessageData);
            ClientRecvText += text + "\r\n";
        }
        #endregion

        #region Client Operations

        public void SendGroupMessage()
        {
            if (_useMulticast && _udpClient.IsRunning)
            {
                byte[] messageData = _udpClient.TextEncoding.GetBytes(ClientSendText.Trim());
                _udpClient.SendGroupMessage(messageData, 9001);
            }
        }

        public override void ClientSend()
        {
            if (!string.IsNullOrWhiteSpace(ClientSendText) && _udpClient.IsRunning)
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9001);
                _udpClient.SendText(ipEndPoint, ClientSendText.Trim());
            }
        }

        public override void ConnectToServer()
        {
            if (CanConnect)
            {
                CanConnect = false;
                if (_useMulticast)
                {
                    _udpClient.Start("224.1.1.1", 9002);
                }
                else
                {
                    _udpClient.Start(9002);
                }
                ClientStatus = ClientStatus.Connected;
            }
        }

        public override void Disconnect()
        {
            _udpClient.Stop();
            CanConnect = true;
            ClientStatus = ClientStatus.Closed;
        }

        #endregion
    }
}
