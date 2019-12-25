using SiS.Communication.Spliter;
using SiS.Communication.Tcp;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Linq;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Represents a simple TCP proxy that support data filter.
    /// </summary>
    public class TcpProxyChannel
    {
        #region Constructor
        /// <summary>
        /// Create an instance of TcpProxyChannel
        /// </summary>
        public TcpProxyChannel()
        {
            _server = new TcpServer(RawPacketSpliter.Default);
            _server.MessageReceived += _server_MessageReceived;
            _server.ClientStatusChanged += _server_ClientStatusChanged;

            _clientDict = new ConcurrentDictionary<long, TcpClientEx>();
            _waitConnDict = new ConcurrentDictionary<long, ManualResetEvent>();
        }
        #endregion

        #region Private Members

        private TcpServer _server;
        private ConcurrentDictionary<long, TcpClientEx> _clientDict;
        private ConcurrentDictionary<long, ManualResetEvent> _waitConnDict;

        #endregion

        #region Events
        /// <summary>
        /// Represents the method that will handle the client count changed event of a SiS.Communication.Business.TcpProxyChannel object.
        /// </summary>
        public event ClientCountChangedEventHandler ClientCountChanged;
        #endregion

        #region Properties

        private string _remoteIP;
        /// <summary>
        /// Gets or sets a value that indicates the ip address of remote server
        /// </summary>
        public string RemoteIP
        {
            get
            {
                return _remoteIP;
            }
            set
            {
                if (IsRunning)
                {
                    throw new Exception("Can not set RemoteIP during running time.");
                }
                _remoteIP = value;
            }
        }

        private int _remotePort = -1;
        /// <summary>
        /// Gets or sets a value that indicates the port of remote server
        /// </summary>
        public int RemotePort
        {
            get
            {
                return _remotePort;
            }
            set
            {
                if (IsRunning)
                {
                    throw new Exception("Can not set RemotePort during running time.");
                }
                _remotePort = value;
            }
        }

        private int _listenPort = -1;
        /// <summary>
        /// Gets or sets a value indicats the listening port of the proxy.
        /// </summary>
        public int ListenPort
        {
            get
            {
                return _listenPort;
            }
            set
            {
                if (IsRunning)
                {
                    throw new Exception("Can not set ListenPort during running time.");
                }
                _listenPort = value;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the proxy is running.
        /// </summary>
        public bool IsRunning { get { return _server.IsRunning; } }

        /// <summary>
        /// Gets or sets data filtering interface
        /// </summary>
        public ITcpProxyDataFilter DataFilter { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates the count of clients.
        /// </summary>
        public int ClientCount { get; private set; } = 0;
        #endregion

        #region Private functions

        private void _server_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
        {
            if (!IsRunning)
            {
                return;
            }
            long proxyClientID = args.ClientID;
            if (args.Status == ClientStatus.Connected)
            {
                TcpClientEx tcpClient = new TcpClientEx(RawPacketSpliter.Default);
                tcpClient.Tag = proxyClientID;
                tcpClient.ClientStatusChanged += TcpClient_ClientStatusChanged;
                tcpClient.MessageReceived += TcpClient_MessageReceived;
                tcpClient.ConnectAsync(RemoteIP, (ushort)RemotePort, (isConnected) =>
                 {
                     if (isConnected)
                     {
                         bool isOK = _clientDict.TryAdd(proxyClientID, tcpClient);
                         System.Diagnostics.Debug.Assert(isOK, "add new client failed");
                     }
                     else
                     {
                         _server.CloseClient(proxyClientID);
                     }
                     if (_waitConnDict.ContainsKey(proxyClientID))
                     {
                         _waitConnDict[proxyClientID].Set();
                     }
                 });
            }
            else if (args.Status == ClientStatus.Closed)
            {
                if (_clientDict.TryRemove(proxyClientID, out TcpClientEx client))
                {
                    client.Close();
                }
            }
            ClientCount = _server.Clients.Count;
            if (ClientCountChanged != null)
            {
                ClientCountChangedEventArgs countChangedArgs = new ClientCountChangedEventArgs() { NewCount = ClientCount };
                ClientCountChanged(this, countChangedArgs);
            }
        }

        private void DebugMessage(string strInfo)
        {
            //System.Diagnostics.Trace.WriteLine(strInfo);
        }

        private void _server_MessageReceived(object sender, TcpRawMessageReceivedEventArgs args)
        {
            if (!IsRunning)
            {
                return;
            }
            long proxyClientID = args.Message.ClientID;
            if (!_clientDict.ContainsKey(proxyClientID))
            {
                //If remote connection is not ready, we have to wait until connection completed
                _waitConnDict.TryRemove(proxyClientID, out ManualResetEvent temp);
                ManualResetEvent waitEvent = new ManualResetEvent(false);
                _waitConnDict.TryAdd(proxyClientID, waitEvent);
                for (int i = 0; i < 300; i++)
                {
                    if (!IsRunning)
                    {
                        return;
                    }
                    if (_clientDict.ContainsKey(proxyClientID))
                    {
                        _waitConnDict.TryRemove(proxyClientID, out ManualResetEvent temp2);
                        break;
                    }
                    else
                    {
                        if (waitEvent.WaitOne(10))
                        {
                            break;
                        }
                    }
                }
            }

            if (_clientDict.ContainsKey(proxyClientID))
            {
                if (DataFilter != null)
                {
                    DataFilter.BeforeClientToServer(args.Message);
                }
                try
                {
                    //the method may throw exception when stopping, so try catch is used but do nothing.
                    _clientDict[proxyClientID].SendMessage(args.Message.MessageRawData.ToArray());
                }
                catch { }
            }
        }

        private void TcpClient_MessageReceived(object sender, TcpRawMessageReceivedEventArgs args)
        {
            if (!IsRunning)
            {
                return;
            }
            TcpClientEx clientEx = sender as TcpClientEx;
            long proxyClientID = (long)clientEx.Tag;
            if (DataFilter != null)
            {
                DataFilter.BeforeServerToClient(new TcpRawMessage
                {
                    ClientID = proxyClientID,
                    MessageRawData = args.Message.MessageRawData
                });
            }
            try
            {
                //the method may throw exception when stopping, so try catch is used but do nothing.
                _server.SendMessage(proxyClientID, args.Message.MessageRawData.ToArray());
            }
            catch { }
        }

        private void TcpClient_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
        {
            if (!IsRunning)
            {
                return;
            }
            TcpClientEx clientEx = sender as TcpClientEx;
            long proxyClientID = (long)clientEx.Tag;
            if (args.Status == ClientStatus.Closed)
            {
                if (_clientDict.TryRemove(proxyClientID, out TcpClientEx client))
                {
                    _server.CloseClient(proxyClientID);
                }
            }
        }

        #endregion

        #region Public functions

        /// <summary>
        /// Start proxy channel
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                throw new AlreadyRunningException("The proxy channel is already running");
            }
            if (RemotePort < 1 || RemotePort > 65535)
            {
                throw new ArgumentException("The remote port is invalid");
            }
            if (ListenPort < 1 || ListenPort > 65535)
            {
                throw new ArgumentException("The listen port is invalid");
            }
            if (!IPAddress.TryParse(RemoteIP, out IPAddress ipAddress))
            {
                throw new ArgumentException("The remote ip address is invalid");
            }
            _server.Start((ushort)ListenPort);
        }

        /// <summary>
        /// Stop proxy channel
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            _server.Stop();
            foreach (TcpClientEx clientEx in _clientDict.Values)
            {
                clientEx.Close();
            }
            _waitConnDict.Clear();
            _clientDict.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Represents the method that will handle the tcp MessageReceived event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">A SiS.Communication.Business.ClientCountChangedEventArgs object that contains the client count.</param>
    public delegate void ClientCountChangedEventHandler(object sender, ClientCountChangedEventArgs args);

    /// <summary>
    /// Provides data for client count changed event.
    /// </summary>
    public class ClientCountChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new count of clients.
        /// </summary>
        public int NewCount { get; set; }
    }

    /// <summary>
    /// The interface used for data filter.
    /// </summary>
    public interface ITcpProxyDataFilter
    {
        /// <summary>
        /// The function will be called before client send message to server.
        /// </summary>
        /// <param name="clientMessage"></param>
        void BeforeClientToServer(TcpRawMessage clientMessage);
        /// <summary>
        /// The function will be called before server send message to client.
        /// </summary>
        /// <param name="serverMessage"></param>
        void BeforeServerToClient(TcpRawMessage serverMessage);
    }
}
