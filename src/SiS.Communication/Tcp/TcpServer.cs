using SiS.Communication.Spliter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents TCP server based on SocketAsyncEvent(IOCP)
    /// </summary>
    public class TcpServer : TcpBase
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Tcp.TcpServer.The default spliter is SimplePacketSpliter().
        /// </summary>
        public TcpServer() : this(new SimplePacketSpliter())
        {

        }

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Tcp.TcpServer using specific packet spliter.
        /// </summary>
        /// <param name="packetSpliter">The spliter used to split stream data into complete packets.</param>
        public TcpServer(IPacketSpliter packetSpliter)
        {
            _packetSpliter = packetSpliter;
        }
        #endregion

        #region Private members

        // private TcpServerConfig _serverConfig;//server parameter
        private ushort _listenPort; //listen port
        private Socket _serverSocket; //server listen socket
        private ConcurrentDictionary<string, HashSet<long>> _groups;

        #endregion

        #region Properties

        private TcpServerConfig ServerConfig
        {
            get { return (TcpServerConfig)_tcpConfig; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable group feature.
        /// </summary>
        /// <returns>true if enable group feature; otherwise, false. The default is false.</returns>
        public bool EnableGroup { get; set; } = false;

        /// <summary>
        /// Get all clients.
        /// </summary>
        public ConcurrentDictionary<long, ClientContext> Clients { get { return _clients; } }

        #endregion

        #region Events
        /// <summary>
        /// Represents the method that will handle the client status changed event of a SiS.Communication.Tcp.TcpServer object.
        /// </summary>
        public event ClientStatusChangedEventHandler ClientStatusChanged;
        /// <summary>
        /// Represents the method that will handle the message received event of a SiS.Communication.Tcp.TcpServer object.
        /// </summary>
        public event TcpRawMessageReceivedEventHandler MessageReceived;
        #endregion

        #region Private functions

        protected override void OnClientStatusChanged(bool isInThread, ClientStatusChangedEventArgs args, HashSet<string> clientGroups)
        {
            if (args.Status == ClientStatus.Closed)
            {
                RemoveClientFromGroup(args.ClientID, clientGroups);
            }
            ClientStatusChanged?.Invoke(this, args);
        }

        protected override void OnRawMessageReceived(TcpRawMessageReceivedEventArgs args)
        {
            ArraySegment<byte> rawSegment = args.Message.MessageRawData;
            if(args.Error == null)
            {
                if (rawSegment.Count >= 4)
                {
                    UInt32 specialMark = BitConverter.ToUInt32(rawSegment.Array, rawSegment.Offset);
                    if (specialMark == TcpUtility.JOIN_GROUP_MARK)
                    {
                        if (!EnableGroup)
                        {
                            return;
                        }

                        JoinGroupMessage joinGroupMsg = null;
                        if (!JoinGroupMessage.TryParse(rawSegment, out joinGroupMsg))
                        {
                            return;
                        }

                        if (_clients.TryGetValue(args.Message.ClientID, out ClientContext clientContext))
                        {
                            clientContext.Groups = joinGroupMsg.GroupSet;
                            AddClientToGroup(clientContext);
                        }
                        return;
                    }
                    else if (specialMark == TcpUtility.GROUP_TRANSMIT_MSG_MARK
                        || specialMark == TcpUtility.GROUP_TRANSMIT_MSG_LOOP_BACK_MARK)
                    {
                        bool loopBack = specialMark == TcpUtility.GROUP_TRANSMIT_MSG_LOOP_BACK_MARK;
                        if (!EnableGroup)
                        {
                            return;
                        }
                        GroupTransmitMessage transPacket = null;
                        if (!GroupTransmitMessage.TryParse(rawSegment, out transPacket))
                        {
                            return;
                        }
                        try
                        {
                            if (!ServerConfig.AllowCrossGroupMessage)
                            {
                                ClientContext sourceClient = GetClient(args.Message.ClientID);
                                if (sourceClient == null || sourceClient.Groups == null || sourceClient.Groups.Count == 0)
                                {
                                    return;
                                }
                                foreach (string groupName in transPacket.GroupNameCollection)
                                {
                                    if (!sourceClient.Groups.Contains(groupName))
                                    {
                                        return;
                                    }
                                }
                            }
                            SendGroupMessageAsync(transPacket.GroupNameCollection, transPacket.TransMessageData.Array, transPacket.TransMessageData.Offset, transPacket.TransMessageData.Count
                                , loopBack ? -1 : args.Message.ClientID);
                        }
                        catch (Exception ex)
                        {
                            _logger?.Warn("Transmit group message exception.", ex.Message);
                        }
                        return;
                    }
                }
                if (ReceivedMessageFilter(args))
                {
                    return;
                }
            }
            
            MessageReceived?.Invoke(this, args);
            return;
        }

        private void RemoveClientFromGroup(long clientID, HashSet<string> groups)
        {
            if (groups == null)
            {
                return;
            }
            foreach (string groupName in groups)
            {
                if (_groups.TryGetValue(groupName, out HashSet<long> clients))
                {
                    if (clients.Contains(clientID))
                    {
                        clients.Remove(clientID);
                    }
                }
            }
        }

        private void AddClientToGroup(ClientContext clientContext)
        {
            foreach (string groupName in clientContext.Groups)
            {
                HashSet<long> clients = null;
                if (!_groups.ContainsKey(groupName))
                {
                    clients = new HashSet<long>();
                    _groups.TryAdd(groupName, clients);
                }
                else
                {
                    clients = _groups[groupName];
                }
                clients.Add(clientContext.ClientID);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs sockAsyncEventArgs)
        {
            if (!_isRunning)
            {
                return;
            }
            if (sockAsyncEventArgs.SocketError == SocketError.Success)
            {
                Socket clientSocket = sockAsyncEventArgs.AcceptSocket;
                if (clientSocket.Connected)
                {
                    //ClientsHandler handler = GetFreeClientsHandler();
                    //ClientContext newClientContext = handler.AddNewClient(clientSocket);
                    AddNewClient(clientSocket);
                }
            }
            if (sockAsyncEventArgs.SocketError != SocketError.OperationAborted)
            {
                StartAccept(sockAsyncEventArgs);
            }
        }

        private void StartAccept(SocketAsyncEventArgs sockAsyncEventArgs)
        {
            if (!_isRunning)
            {
                return;
            }
            if (sockAsyncEventArgs == null)
            {
                sockAsyncEventArgs = new SocketAsyncEventArgs();
                sockAsyncEventArgs.Completed += (sender, sockAsyncArgs) =>
                {
                    ProcessAccept(sockAsyncArgs);
                };
            }
            else
            {
                //socket must be cleared since the context object is being reused
                sockAsyncEventArgs.AcceptSocket = null;
            }
            try
            {
                if (!_serverSocket.AcceptAsync(sockAsyncEventArgs))
                {
                    ProcessAccept(sockAsyncEventArgs);
                }
            }
            catch
            {
                return;
            }
        }

        private void SendGroupMessageAsync(IEnumerable<string> groupNameCollection, byte[] messageData, int offset, int count, long exceptClientID)
        {
            if (!EnableGroup)
            {
                throw new Exception("can not send group message when EnableGroup property is false");
            }
            //byte[] packetForSend = 
            ArraySegment<byte>? packetForSend = null; //
            foreach (string groupName in groupNameCollection)
            {
                if (_groups.TryGetValue(groupName, out HashSet<long> clients))
                {
                    foreach (long clientID in clients)
                    {
                        if (clientID != exceptClientID && _clients.TryGetValue(clientID, out ClientContext clientContext))
                        {
                            if (packetForSend == null)
                            {
                                packetForSend = _packetSpliter.MakePacket(messageData, offset, count, clientContext.SendBuffer);
                            }
                            clientContext.ClientSocket.BeginSend(packetForSend.Value.Array, packetForSend.Value.Offset, packetForSend.Value.Count, SocketFlags.None, null, null);
                        }
                    }
                }
            }

        }
        #endregion

        #region Public functions

        /// <summary>
        /// Start tcp server listening on a specific port.
        /// </summary>
        /// <param name="nPort">The listening port of the server.</param>
        public void Start(ushort listenPort)
        {
            Start(listenPort, TcpServerConfig.Default);
        }

        /// <summary>
        /// Start udp server listening on a specific port using TcpServerParam parameter.
        /// </summary>
        /// <param name="listenPort">The listening port of the server.</param>
        /// <param name="serverConfig">The server's parameter, see TcpServerParam.</param>
        public void Start(ushort listenPort, TcpServerConfig serverConfig)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException("the server is already running");
            }
            // _clientContextPool = new ClientContextPool(serverConfig.MaxClientCount, serverConfig.SocketAsyncBufferSize);
            Contract.Requires(listenPort > 0 && listenPort < 65535);
            Contract.Requires(serverConfig != null);
            _tcpConfig = serverConfig;
            _listenPort = listenPort;
            try
            {
                //create listen socket
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (ServerConfig.EnableKeepAlive)
                {
                    TcpUtility.SetKeepAlive(_serverSocket, ServerConfig.KeepAliveTime, ServerConfig.KeepAliveInterval);
                }
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, listenPort));
                _serverSocket.Listen(ServerConfig.MaxPendingCount);
                _groups = new ConcurrentDictionary<string, HashSet<long>>();
            }
            catch (Exception ex)
            {
                throw new Exception("Start tcp server failed", ex);
            }

            _isRunning = true;
            StartAccept(null);
        }

        /// <summary>
        /// Stop the server and release all the resources.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            _serverSocket.Close();
            _serverSocket.Dispose();
            _serverSocket = null;
            foreach (ClientContext clientContext in _clients.Values)
            {
                IPEndPoint ipEndPt = clientContext.IPEndPoint;
                clientContext.Status = ClientStatus.Closed;
                clientContext.ClientSocket.Close();
                clientContext.ClientSocket.Dispose();
            }
            _clients.Clear();
            if (_clientContextPool != null)
            {
                _clientContextPool.Clear();
            }
            _groups.Clear();
        }

        /// <summary>
        /// Get one client context by client id.
        /// </summary>
        /// <param name="clientID">The client id in long type.</param>
        /// <returns>The client context if the client exist; otherwise, null.</returns>
        public ClientContext GetClient(long clientID)
        {
            if (!_isRunning)
            {
                throw new Exception("the server is not running");
            }
            ClientContext clientContext = null;
            _clients.TryGetValue(clientID, out clientContext);
            return clientContext;
        }

        /// <summary>
        /// Gets clients from specific group.
        /// </summary>
        /// <param name="groupName">The group of the clients.</param>
        /// <returns>The list of the client context.</returns>
        public List<ClientContext> GetGroupClients(string groupName)
        {
            List<ClientContext> results = new List<ClientContext>();
            if (_groups.TryGetValue(groupName, out HashSet<long> clients))
            {
                foreach (long clientID in clients)
                {
                    if (_clients.TryGetValue(clientID, out ClientContext clientContext))
                    {
                        results.Add(clientContext);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Close client connection by client id.
        /// </summary>
        /// <param name="clientID">The client id to disconnect from the server.</param>
        public void CloseClient(long clientID)
        {
            base.CloseClient(false, clientID);
        }

        /// <summary>
        /// Send message data to specific client in synchronous mode.
        /// </summary>
        /// <param name="clientID">The client id to receive message.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendMessage(long clientID, byte[] messageData)
        {
            return SendMessage(clientID, messageData, 0, messageData.Length);
        }

        /// <summary>
        /// Send message data to specific client in synchronous mode.
        /// </summary>
        /// <param name="clientID">The client id to receive message.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendMessage(long clientID, byte[] messageData, int offset, int count)
        {
            if (!_isRunning)
            {
                throw new Exception("the server is not running");
            }

            ClientContext clientContext = null;
            _clients.TryGetValue(clientID, out clientContext);
            if (clientContext == null)
            {
                throw new Exception("the client is not exist");
            }

            if (clientContext.Status != ClientStatus.Connected)
            {
                throw new Exception("the client is not connected");
            }
            ArraySegment<byte> packetForSend = _packetSpliter.MakePacket(messageData, offset, count, clientContext.SendBuffer);
            int sendTotal = 0;
            int sendIndex = packetForSend.Offset;
            while (sendTotal < packetForSend.Count)
            {
                int single = clientContext.ClientSocket.Send(packetForSend.Array, sendIndex, packetForSend.Count - sendTotal, SocketFlags.None);
                sendTotal += single;
                sendIndex += single;
            }
            return sendTotal;
        }

        /// <summary>
        /// Send message data to specific client in asynchronous mode.
        /// </summary>
        /// <param name="clientID">The client id to receive message.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult that references the asynchronous send.</returns>
        public IAsyncResult SendMessageAsync(long clientID, byte[] messageData, AsyncCallback callback)
        {
            return SendMessageAsync(clientID, messageData, 0, messageData.Length, callback);
        }

        /// <summary>
        /// Send message data to specific client in asynchronous mode.
        /// </summary>
        /// <param name="clientID">The client id to receive message.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult that references the asynchronous send.</returns>
        public IAsyncResult SendMessageAsync(long clientID, byte[] messageData, int offset, int count, AsyncCallback callback)
        {
            if (!_isRunning)
            {
                throw new Exception("the server is not running");
            }
            ClientContext clientContext = null;// GetClient(clientID);
            _clients.TryGetValue(clientID, out clientContext);
            if (clientContext == null)
            {
                throw new Exception("the client is not exist");
            }
            if (clientContext.Status != ClientStatus.Connected)
            {
                throw new Exception("the client is not connected");
            }
            return SendMessageAsync(new List<long>() { clientID }, messageData, offset, count, callback).FirstOrDefault();
        }

        /// <summary>
        /// Send message data to specific clients in asynchronous mode.
        /// </summary>
        /// <param name="clientIDCollection">The client's id collection to receive message.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public IEnumerable<IAsyncResult> SendMessageAsync(IEnumerable<long> clientIDCollection, byte[] messageData, int offset, int count, AsyncCallback callback)
        {
            if (!_isRunning)
            {
                throw new Exception("the server is not running");
            }
            //byte[] packetForSend = _packetSpliter.MakePacket(messageData, offset, count);
            ArraySegment<byte>? packetForSend = null;
            List<IAsyncResult> asyncResults = new List<IAsyncResult>();
            foreach (long clientID in clientIDCollection)
            {
                ClientContext clientContext = null;
                if (!_clients.TryGetValue(clientID, out clientContext)
                    || clientContext.Status != ClientStatus.Connected)
                {
                    continue;
                }
                try
                {
                    if (packetForSend == null)
                    {
                        packetForSend = _packetSpliter.MakePacket(messageData, offset, count, clientContext.SendBuffer);
                    }
                    IAsyncResult iAsyncResult = clientContext.ClientSocket.BeginSend(packetForSend.Value.Array, packetForSend.Value.Offset,
                        packetForSend.Value.Count, SocketFlags.None, callback, clientContext);
                    asyncResults.Add(iAsyncResult);
                }
                catch { }
            }
            return asyncResults;
        }

        /// <summary>
        /// Send message data to specific clients in asynchronous mode.
        /// </summary>
        /// <param name="clientIDCollection">The client's id collection to receive message.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public IEnumerable<IAsyncResult> SendMessageAsync(IEnumerable<long> clientIDCollection, byte[] messageData, AsyncCallback callback)
        {
            return SendMessageAsync(clientIDCollection, messageData, 0, messageData.Length, callback);
        }

        /// <summary>
        /// Send message data to specific group collection in asynchronous mode.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="messageData">The message data to be sent to group collection.</param>
        public void SendGroupMessageAsync(IEnumerable<string> groupNameCollection, byte[] messageData)
        {
            SendGroupMessageAsync(groupNameCollection, messageData, 0, messageData.Length);
        }

        /// <summary>
        /// Send message data to specific group collection in asynchronous mode.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="messageData">The message data to be sent to group collection.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        public void SendGroupMessageAsync(IEnumerable<string> groupNameCollection, byte[] messageData, int offset, int count)
        {
            if (!_isRunning)
            {
                throw new Exception("the server is not running");
            }
            SendGroupMessageAsync(groupNameCollection, messageData, offset, count, -1);
        }

        /// <summary>
        /// Send message data to all clients in asynchronous mode.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public IEnumerable<IAsyncResult> BroadcastMessage(byte[] messageData, AsyncCallback callback)
        {
            return BroadcastMessage(messageData, 0, messageData.Length, callback);
        }

        /// <summary>
        /// Send message data to all clients in asynchronous mode.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public IEnumerable<IAsyncResult> BroadcastMessage(byte[] messageData, int offset, int count, AsyncCallback callback)
        {
            if (!_isRunning)
            {
                throw new Exception("the server is not running");
            }
            return SendMessageAsync(_clients.Keys.ToList(), messageData, offset, count, callback);
        }

        /// <summary>
        /// Send message text to specific client in synchronous mode with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="clientID">The client id to send messsage.</param>
        /// <param name="text">The message text to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public void SendText(long clientID, string text)
        {
            SendMessage(clientID, TextEncoding.GetBytes(text));
        }

        /// <summary>
        /// Send message text to specific client in asynchronous mode with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="clientID">The id of the client to receive messsage.</param>
        /// <param name="text">The message text to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult that references the asynchronous send.</returns>
        public IAsyncResult SendTextAsync(long clientID, string text, AsyncCallback callback)
        {
            return SendMessageAsync(clientID, TextEncoding.GetBytes(text), callback);
        }

        /// <summary>
        /// Send message text to specific clients in asynchronous mode with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="clientIDCollection">The clients' id collection to receive messsage.</param>
        /// <param name="text">The message text to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public IEnumerable<IAsyncResult> SendTextAsync(IEnumerable<long> clientIDCollection, string text, AsyncCallback callback)
        {
            return SendMessageAsync(clientIDCollection, TextEncoding.GetBytes(text), callback);
        }

        /// <summary>
        /// Send message text to specific group collection in asynchronous mode with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="text">The message text to be sent.</param>
        public void SendGroupTextAsync(IEnumerable<string> groupNameCollection, string text)
        {
            SendGroupMessageAsync(groupNameCollection, TextEncoding.GetBytes(text));
        }

        /// <summary>
        /// Send message text to all clients in asynchronous mode.
        /// </summary>
        /// <param name="text">The message text to be sent.</param>
        /// <param name="callback">The callback that will be called after sending operation.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public IEnumerable<IAsyncResult> BroadcastText(string text, AsyncCallback callback)
        {
            return BroadcastMessage(TextEncoding.GetBytes(text), callback);
        }

        #endregion
    }
}
