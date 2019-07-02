using SiS.Communication.Business;
using SiS.Communication.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Demo
{
    public class TcpModelViewModel : PageBaseViewModel
    {
        #region Constructor

        public TcpModelViewModel(string title) : base(title)
        {
            _serverVM = new TcpModelServerViewModel();
            _clientVM = new TcpModelClientViewModel();
            _clientVM.ShowAutoReconnect = false;
        }
        public TcpModelViewModel() : this("Tcp Model")
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

    public class TcpModelServerViewModel : ServerBaseViewModel
    {
        #region Constructor

        public TcpModelServerViewModel()
        {
            _tcpServer = new TcpModelServer((int)MessageHeader.Model, JsonModelMessageConvert.Default);
            _tcpServer.ClientStatusChanged += OnTcpServer_ClientStatusChanged;
            _tcpServer.MessageReceived += OnTcpServer_MessageReceived;

            ServerSendText = "I am server";
        }

        #endregion

        #region Private Members

        protected TcpModelServer _tcpServer;
        protected const ushort _serverPort = 9999;
        protected long _serverClientID = -1;

        #endregion

        #region Server Event Handlers

        protected virtual void OnTcpServer_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
        {
            if (args.Status == ClientStatus.Connected)
            {
                _serverClientID = args.ClientID;
            }
            else
            {
                _serverClientID = -1;
            }
        }
        protected virtual void OnTcpServer_MessageReceived(object sender, TcpRawMessageReceivedEventArgs args)
        {
            GeneralMessage clientMessage = GeneralMessage.Deserialize(args.Message.MessageRawData, false);

            //if the server message is model
            if (clientMessage.Header == (int)MessageHeader.Model)
            {
                //get model from general message
                object model = _tcpServer.ConvertToModel(clientMessage);
                if (model is QueryServerTimeRequest)
                {
                    QueryServerTimeRequest queryTimeReq = model as QueryServerTimeRequest;
                    QueryServerTimeResponse response = new QueryServerTimeResponse
                    {
                        RequestID = queryTimeReq.ID,
                        ServerTime = DateTime.Now
                    };
                    ServerRecvText += $"Received Request:\r\nfrom {queryTimeReq.Name}\r\n";
                    _tcpServer.SendModelMessage(args.Message.ClientID, response);
                }
                else
                {
                    //do something for other model
                }
            }
            else
            {

            }
        }

        #endregion

        #region Server Operations

        public override void ServerSend()
        {
            if (_serverClientID > 0)
            {
                if (!string.IsNullOrWhiteSpace(ServerSendText))
                {
                    //_tcpServer.SendText(_serverClientID, ServerSendText.Trim());
                    ServerMessage serverMessage = new ServerMessage()
                    {
                        Title = "test",
                        Body = ServerSendText.Trim()
                    };

                    _tcpServer.SendModelMessage(_serverClientID, serverMessage);
                }
            }
        }

        public override void StartServer()
        {
            if (CanStartServer)
            {
                _tcpServer.Start(_serverPort);
                CanStartServer = false;
            }
        }

        public override void StopServer()
        {
            _tcpServer.Stop();
            CanStartServer = true;
        }

        #endregion
    }

    public class TcpModelClientViewModel : ClientBaseViewModel
    {
        #region Constructor

        public TcpModelClientViewModel()
        {
            _tcpClient = new TcpModelClient(true, (int)MessageHeader.Model, JsonModelMessageConvert.Default);
            _tcpClient.ClientStatusChanged += OnTcpCient_ClientStatusChanged;
            _tcpClient.MessageReceived += OnTcpClient_PacketReceived;

            ClientSendText = "I am client";
        }

        #endregion

        #region Private Members
        protected TcpModelClient _tcpClient;
        private const ushort _serverPort = 9999;

        #endregion


        #region Client Event Handlers

        protected virtual void OnTcpCient_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
        {
            ClientStatus = args.Status;
            if (args.Status == ClientStatus.Closed)
            {
                CanConnect = true;
            }
        }

        protected virtual void OnTcpClient_PacketReceived(object sender, TcpRawMessageReceivedEventArgs args)
        {
            GeneralMessage serverMessage = GeneralMessage.Deserialize(args.Message.MessageRawData, false);

            //if the server message is model
            if (serverMessage.Header == (int)MessageHeader.Model)
            {
                //get model from general message
                object model = _tcpClient.ConvertToModel(serverMessage);
                if (model is ServerMessage)
                {
                    ServerMessage serverMsg = model as ServerMessage;
                    ClientRecvText += "received msg: " + serverMsg.Title + "," + serverMsg.Body + "\r\n";
                }
                else
                {
                    //do something for other model
                }
            }
            else
            {

            }
        }

        #endregion

        #region Client Operations

        public override void ClientSend()
        {
            if (!string.IsNullOrWhiteSpace(ClientSendText))
            {
                //_tcpClient.SendText(ClientSendText.Trim());
                QueryServerTimeRequest request = new QueryServerTimeRequest()
                {
                    Name = "jackson",
                    Message = ClientSendText
                };
                QueryServerTimeResponse response = _tcpClient.QueryAsync<QueryServerTimeResponse>(request).Result;
                ClientRecvText += "server time: " + response.ServerTime.ToString() + "\r\n";
            }
        }

        public override void ConnectToServer()
        {
            if (CanConnect)
            {
                CanConnect = false;
                _tcpClient.AutoReconnect = EnableAutoReconnect;
                _tcpClient.ConnectAsync("127.0.0.1", _serverPort, (isConnected) =>
                {
                    CanConnect = !isConnected;
                });
            }
        }

        public override void Disconnect()
        {
            _tcpClient.Close();
        }


        #endregion
    }

    public enum MessageHeader : int
    {
        Model = 1,
        Other
    }
}
