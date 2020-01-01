using SiS.Communication.Http;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SiS.Communication.Demo
{
    public class HttpViewModel : PageBaseViewModel
    {
        #region Constructor
        public HttpViewModel(string title) : base(title)
        {
            _clientList = new ObservableCollection<IClientContext>();
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            _httpServer = new HttpServer();
            _websocketHandler = new WebsocketHandler("/ws");
            _staticFileHandler = new StaticFileHandler();
            _staticFileHandler.DefaultFiles.Add("index.html");
            _httpServer.Handlers.Add(_websocketHandler);
            _httpServer.Handlers.Add(_staticFileHandler);
            _httpServer.UnhandledRequestReceived += _httpServer_UnhandledRequestReceived; ;
            _httpServer.WebSocketDataReceived += _httpServer_WSDataReceived;
            _httpServer.WebSocketStatusChanged += _httpServer_WebSocketStatusChanged;
        }

        public HttpViewModel() : this("Http")
        {

        }
        #endregion

        #region Privte members
        private Dispatcher _uiDispatcher;
        private HttpServer _httpServer;
        private StaticFileHandler _staticFileHandler;
        private WebsocketHandler _websocketHandler;
        #endregion

        #region Properties

        private IClientContext _selectedClient;
        public IClientContext SelectedClient
        {
            get { return _selectedClient; }
            set
            {
                if (value != _selectedClient)
                {
                    _selectedClient = value;
                    NotifyPropertyChanged(nameof(SelectedClient));
                }
            }
        }

        private ObservableCollection<IClientContext> _clientList;
        public ObservableCollection<IClientContext> ClientList
        {
            get { return _clientList; }
            set
            {
                if (value != _clientList)
                {
                    _clientList = value;
                    NotifyPropertyChanged(nameof(ClientList));
                }
            }
        }

        private string _webRoot;
        public string WebRoot
        {
            get { return _webRoot; }
            set
            {
                if (value != _webRoot)
                {
                    _webRoot = value;
                    NotifyPropertyChanged(nameof(WebRoot));
                }
            }
        }

        private int _port = 8080;
        public int Port
        {
            get { return _port; }
            set
            {
                if (value != _port)
                {
                    _port = value;
                    NotifyPropertyChanged(nameof(Port));
                }
            }
        }

        private bool _canStartServer = true;
        public bool CanStartServer
        {
            get { return _canStartServer; }
            set
            {
                if (value != _canStartServer)
                {
                    _canStartServer = value;
                    NotifyPropertyChanged(nameof(CanStartServer));
                }
            }
        }

        private string _serverSendText;
        public string ServerSendText
        {
            get { return _serverSendText; }
            set
            {
                if (value != _serverSendText)
                {
                    _serverSendText = value;
                    NotifyPropertyChanged(nameof(ServerSendText));
                }
            }
        }

        private string _serverRecvText;
        public string ServerRecvText
        {
            get { return _serverRecvText; }
            set
            {
                if (value != _serverRecvText)
                {
                    _serverRecvText = value;
                    NotifyPropertyChanged(nameof(ServerRecvText));
                }
            }
        }

        private string _webSocketUrl;
        public string WebSocketUrl
        {
            get { return _webSocketUrl; }
            set
            {
                if (value != _webSocketUrl)
                {
                    _webSocketUrl = value;
                    NotifyPropertyChanged(nameof(WebSocketUrl));
                }
            }
        }


        private bool _enableGZIP = true;
        public bool EnableGZIP
        {
            get { return _enableGZIP; }
            set
            {
                if (value != _enableGZIP)
                {
                    _enableGZIP = value;
                    NotifyPropertyChanged(nameof(EnableGZIP));
                }
            }
        }


        #endregion

        #region  Event handlers

        private void _httpServer_WSDataReceived(object sender, WebSocketDataReceivedEventArgs args)
        {
            WebSocketPacket wspacket = args.DataPacket;
            if (wspacket.DataType == WSPacketType.Text)
            {
                string strText = Encoding.UTF8.GetString(wspacket.Data.Array, wspacket.Data.Offset, wspacket.Data.Count);
                string strRecv = $"[{args.ClientID}] {strText}\r\n";
                ServerRecvText += strRecv;
            }

        }

        private void _httpServer_WebSocketStatusChanged(object sender, WebSocketStatusChangedEventArgs args)
        {
            if (args.Client.IsConnected)
            {
                _uiDispatcher.BeginInvoke(new Action<IClientContext>((client) =>
                {
                    _clientList.Add(client);
                }), args.Client);
            }
            else
            {
                _uiDispatcher.BeginInvoke(new Action<long>((clientID) =>
                {
                    for (int i = 0; i < _clientList.Count; i++)
                    {
                        if (_clientList[i].ClientID == clientID)
                        {

                            _clientList.RemoveAt(i);
                            break;
                        }
                    }
                }), args.Client.ClientID);

            }
        }

        private void _httpServer_UnhandledRequestReceived(object sender, UnhandledRequestReceivedEventArgs args)
        {
            HttpRequestMessage requestMsg = args.Context.Request;
            if (requestMsg.Content is BlockStreamContent streamContent)
            {
                BlockStream stream = streamContent.Stream;
                FileStream fs = null;
                try
                {
                    fs = File.Open("d:\\test\\test.exe", FileMode.Create, FileAccess.Write);
                    stream.CopyTo(fs);
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }

                HttpResponseMessage responseMsg = ResponseMsgHelper.CreateSimpleRepMsg();
                args.Context.Response = responseMsg;
            }
            else if(requestMsg.Content is ByteArrayContent byteContent)
            {
                
            }
        }

        #endregion

        #region Private functions

        #endregion

        #region Public functions
        public void StartServer()
        {
            if (_httpServer.IsRunning)
            {
                return;
            }
            if (string.IsNullOrEmpty(WebRoot))
            {
                MessageBox.Show("The root dir is required.");
                return;
            }
            if (_port <= ushort.MinValue || _port > ushort.MaxValue)
            {
                MessageBox.Show($"The port must be between 1 and {ushort.MaxValue}");
                return;
            }
            _staticFileHandler.RootDir = WebRoot;
            _staticFileHandler.EnableGZIP = EnableGZIP;
            HttpServerConfig config = new HttpServerConfig();
            config.TcpConfig.MaxPendingCount = 10000;
            config.TcpConfig.MaxClientCount = 2000;
            config.TcpConfig.SocketAsyncBufferSize = 100 * 1024;
            try
            {
                _httpServer.Start((ushort)_port, config);
                CanStartServer = false;
                WebSocketUrl = $"ws://127.0.0.1:{_port}{_websocketHandler.RelativeUrl}";
                AppConfig.Singleton.HttpRootDir = _webRoot;
                AppConfig.Singleton.HttpListenPort = _port;
                AppConfig.Singleton.EnableGZIP = _enableGZIP;
                AppConfig.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void StopServer()
        {
            _httpServer.Stop();
            CanStartServer = true;
            WebSocketUrl = "";
        }

        public void Send()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Please select the client that you want to send message.");
                return;
            }
            if (string.IsNullOrEmpty(ServerSendText))
            {
                MessageBox.Show("Please input the message that you want to send.");
                return;
            }
            try
            {
                _httpServer.WebSocketSendText(SelectedClient, ServerSendText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void Disconnect()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Please select the client that you want to disconnect.");
                return;
            }
            try
            {
                _httpServer.WebSocketCloseClient(SelectedClient);
                _clientList.Remove(SelectedClient);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public override void Initialize()
        {
            WebRoot = AppConfig.Singleton.HttpRootDir;
            Port = AppConfig.Singleton.HttpListenPort;
            EnableGZIP = AppConfig.Singleton.EnableGZIP;
        }
        #endregion
    }
}
