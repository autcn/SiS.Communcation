using SiS.Communication.Spliter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents an object used to handle a set of clients
    /// </summary>
    internal class ClientsHandler
    {
        #region Constructor

        public ClientsHandler(TcpBase tcpBase, TcpConfig tcpConfig, ClientContextPool clientContextPool)
        {
            // _tcpBase = tcpBase;
            _logger = tcpBase.Logger;
            _tcpConfig = tcpConfig;
            _clientContextPool = clientContextPool;
            ID = Guid.NewGuid();
            _packetSpliter = tcpBase.PacketSpliter;
            _clients = new ConcurrentDictionary<long, ClientContext>();
        }

        public ClientsHandler(TcpBase tcpBase, TcpConfig tcpConfig) : this(tcpBase, tcpConfig, null)
        {
        }
        #endregion

        #region Private members

        private TcpConfig _tcpConfig;
        private IPacketSpliter _packetSpliter;
        private ClientContextPool _clientContextPool;
        private SingleThreadTaskScheduler _taskScheduler;
        private ILog _logger;
        #endregion

        #region Properties
        public Guid ID { private set; get; }

        private ConcurrentDictionary<long, ClientContext> _clients;
        public ConcurrentDictionary<long, ClientContext> Clients
        {
            get { return _clients; }
        }

        private bool _isRunning = false;
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public SingleThreadTaskScheduler TaskScheduler
        {
            get { return _taskScheduler; }
        }

        #endregion

        #region Events
        public event ClientStatusChangedEventHandler ClientStatusChanged;
        public event TcpRawMessageReceivedEventHandler MessageReceived;
        #endregion

        #region Private functions


        private void ProcessReceive(SocketAsyncEventArgs sockAsyncArgs)
        {
            if (!_isRunning)
            {
                return;
            }
            long clientID = (long)sockAsyncArgs.UserToken;
            if (!_clients.ContainsKey(clientID))
            {
                return;
            }
            ClientContext clientContext = _clients[clientID];
            Socket sockClient = clientContext.ClientSocket;
            if (sockAsyncArgs.SocketError == SocketError.Success)
            {
                int recvCount = sockAsyncArgs.BytesTransferred;
                if (recvCount > 0)
                {
                    RingQueue clientRingBuffer = clientContext.ReceiveBuffer;
                    clientRingBuffer.Write(sockAsyncArgs.Buffer, sockAsyncArgs.Offset, recvCount);
                    //speed limit
                    if (clientContext.RecvSpeedController != null)
                    {
                        clientContext.RecvSpeedController.TryLimit(recvCount);
                    }

                    //try get a completed packet
                    try
                    {
                        int endPos = 0;
                        List<ArraySegment<byte>> messageList = _packetSpliter.GetPackets(clientRingBuffer.Buffer, 0, clientRingBuffer.DataLength, out endPos);

                        if (messageList != null)
                        {
                            clientRingBuffer.Remove(endPos);
                            foreach (ArraySegment<byte> messageSegment in messageList)
                            {
                                clientContext.RecvRawMessage.HandlerID = this.ID;
                                clientContext.RecvRawMessage.ClientID = (long)sockClient.Handle;
                                clientContext.RecvRawMessage.MessageRawData = messageSegment;
                                MessageReceived?.Invoke(this, new TcpRawMessageReceivedEventArgs()
                                {
                                    Message = clientContext.RecvRawMessage,
                                    Scheduler = _taskScheduler
                                });
                            }
                        }
                    }
                    catch (InvalidPacketException)
                    {
                        //invalid data received , indicates the client has made a illegal connection, we should disconnect it.
                        _logger?.Info("illegal connection detected");
                        CloseClient((long)sockClient.Handle);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warn("an error has occurred during get packets", ex.Message);
                    }

                    if (!clientContext.ClientSocket.ReceiveAsync(sockAsyncArgs))
                    {
                        ProcessReceive(sockAsyncArgs);
                    }
                }
                else
                {
                    _logger?.Warn($"sockAsyncArgs got an error: {sockAsyncArgs.SocketError.ToString()}");
                }
            }
            else
            {
                CloseClient(clientID);
            }
        }

        private void NotifyClientStatusChangedAsync(long clientID, IPEndPoint ipEnd, ClientStatus status)
        {
            //async event
            ClientStatusChangedEventArgs eventArgs = new ClientStatusChangedEventArgs()
            {
                ClientID = clientID,
                IPEndPoint = ipEnd,
                Status = status
            };
            ThreadEx.Start((args) =>
            {
                ClientStatusChanged?.Invoke(this, (ClientStatusChangedEventArgs)args);
            }, eventArgs);
        }

        private void SockAsyncArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                ProcessReceive(e);
            }
        }

        #endregion

        #region Public functions

        public void Start()
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException("the hanlder is already running");
            }
            _clients.Clear();
            _taskScheduler = new SingleThreadTaskScheduler();
            _isRunning = true;
        }

        public ClientContext AddNewClient(Socket newClient)
        {
            ClientContext context = null;
            if (_clientContextPool == null)
            {
                context = new ClientContext();
                context.SockAsyncArgs = new SocketAsyncEventArgs();
                byte[] asyncBuffer = new byte[_tcpConfig.SocketAsyncBufferSize];
                context.SockAsyncArgs.SetBuffer(asyncBuffer, 0, asyncBuffer.Length);
            }
            else
            {
                context = _clientContextPool.Pop();
            }

            context.ClientSocket = newClient;
            context.HandlerID = ID;
            context.ClientID = (long)newClient.Handle;
            context.Status = ClientStatus.Connected;
            if (_tcpConfig.ReceiveDataMaxSpeed > 0)
            {
                context.RecvSpeedController.LimitSpeed = _tcpConfig.ReceiveDataMaxSpeed;
                context.RecvSpeedController.Enabled = true;
            }
            if (_tcpConfig.SendDataMaxSpeed > 0)
            {
                context.SendController.LimitSpeed = _tcpConfig.SendDataMaxSpeed;
                context.SendController.Enabled = true;
            }
            context.SockAsyncArgs.Completed += SockAsyncArgs_Completed;
            IPEndPoint remoteIPEnd = (IPEndPoint)newClient.RemoteEndPoint;
            //save original ip end point, so we can always get the remote ip end point even the socket is closed.
            context.IPEndPoint = new IPEndPoint(remoteIPEnd.Address, remoteIPEnd.Port);
            _clients.TryAdd((long)newClient.Handle, context);
            NotifyClientStatusChangedAsync(context.ClientID, newClient.RemoteEndPoint as IPEndPoint, ClientStatus.Connected);
            context.SockAsyncArgs.UserToken = (long)newClient.Handle;

            if (!newClient.ReceiveAsync(context.SockAsyncArgs))
            {
                Task.Factory.StartNew(() =>
                {
                    ProcessReceive(context.SockAsyncArgs);
                });
            }
            return context;
        }

        public void CloseClient(long clientID)
        {
            CloseClients(new List<long>() { clientID });
        }

        public void CloseClients(IEnumerable<long> clientIDCollection)
        {
            if (clientIDCollection == null || !clientIDCollection.Any())
            {
                return;
            }

            foreach (long clientID in clientIDCollection)
            {
                if (_clients.ContainsKey(clientID))
                {
                    ClientContext clientContext = _clients[clientID];
                    IPEndPoint ipEndPt = clientContext.IPEndPoint;
                    clientContext.Status = ClientStatus.Closed;
                    clientContext.ClientSocket.Close();
                    clientContext.ClientSocket.Dispose();
                    clientContext.SockAsyncArgs.Completed -= SockAsyncArgs_Completed;
                    if (_isRunning)
                    {
                        NotifyClientStatusChangedAsync(clientID, ipEndPt, ClientStatus.Closed);
                    }
                    _clients.TryRemove(clientID, out ClientContext n);
                    if (_clientContextPool != null)
                    {
                        clientContext.Reset();
                        _clientContextPool.Push(clientContext);
                    }
                }
            }
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            IEnumerable<long> clientIDCollection = _clients.Values.Select(p => (long)p.ClientSocket.Handle);
            CloseClients(clientIDCollection);

            _clients.Clear();
            _taskScheduler.Stop();
        }

        #endregion
    }
}
