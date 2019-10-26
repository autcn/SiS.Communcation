using SiS.Communication.Spliter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents a base class of tcp communication
    /// </summary>
    public abstract class TcpBase
    {
        #region Constructor
        public TcpBase()
        {
            _clients = new ConcurrentDictionary<long, ClientContext>();
        }
        #endregion

        #region Private Members
        protected bool _isRunning = false;
        protected IPacketSpliter _packetSpliter;
        protected TcpConfig _tcpConfig;
        protected ClientContextPool _clientContextPool;
        protected ILog _logger;
        protected ConcurrentDictionary<long, ClientContext> _clients;

        #endregion

        #region Events
        public event ClientStatusChangedEventHandler ClientStatusChanged;
        public event TcpRawMessageReceivedEventHandler MessageReceived;
        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the tcp is running.
        /// </summary>
        /// <returns>true if the tcp is running; otherwise, false. The default is false.</returns>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        /// <summary>
        /// Gets or sets an object that implements the ILog interface.
        /// </summary>
        public ILog Logger
        {
            get { return _logger; }
            set
            {
                _logger = value;
            }
        }

        /// <summary>
        /// Get or sets the text encoding in tcp communication.
        /// </summary>
        /// <returns>The text encoding int tcp communication. The default is UTF8.</returns>
        public Encoding TextEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets or sets the packet spliter which is used to split stream data.
        /// </summary>
        /// <returns>The packet spliter.</returns>
        public IPacketSpliter PacketSpliter
        {
            get { return _packetSpliter; }
            set
            {
                if (_isRunning)
                {
                    throw new Exception("can not change packet spliter during running time");
                }
                _packetSpliter = value;
            }
        }
        #endregion


        #region Protected Functions

        protected virtual bool ReceivedMessageFilter(TcpRawMessageReceivedEventArgs tcpRawMessageArgs)
        {
            return false;
        }

        protected virtual void OnClientStatusChanged(ClientStatusChangedEventArgs args)
        {
        }

        protected virtual bool OnRawMessageReceived(TcpRawMessageReceivedEventArgs args)
        {
            return false;
        }

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
                                clientContext.RecvRawMessage.ClientID = (long)sockClient.Handle;
                                clientContext.RecvRawMessage.MessageRawData = messageSegment;
                                TcpRawMessageReceivedEventArgs rawMessage = new TcpRawMessageReceivedEventArgs()
                                {
                                    Message = clientContext.RecvRawMessage
                                };
                                if (!OnRawMessageReceived(rawMessage))
                                {
                                    if (!ReceivedMessageFilter(rawMessage))
                                    {
                                        MessageReceived?.Invoke(this, rawMessage);
                                    }
                                }
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
                    CloseClient(clientID);
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
            //ThreadEx.Start((args) =>
            //{
            OnClientStatusChanged(eventArgs);
            ClientStatusChanged?.Invoke(this, eventArgs);
            //   ClientStatusChanged?.Invoke(this, (ClientStatusChangedEventArgs)args);
            // }, eventArgs);
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

        protected ClientContext AddNewClient(Socket newClient)
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
            context.SockAsyncArgs.UserToken = (long)newClient.Handle;
            NotifyClientStatusChangedAsync(context.ClientID, newClient.RemoteEndPoint as IPEndPoint, ClientStatus.Connected);
            if (!newClient.ReceiveAsync(context.SockAsyncArgs))
            {
                ProcessReceive(context.SockAsyncArgs);
            }
            return context;
        }

        protected void CloseClient(long clientID)
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

        protected void CloseClients(IEnumerable<long> clientIDCollection)
        {
            if (clientIDCollection == null || !clientIDCollection.Any())
            {
                return;
            }

            foreach (long clientID in clientIDCollection)
            {
                CloseClient(clientID);
            }
        }

        protected void CloseAllClients()
        {
            CloseClients(_clients.Keys);
            _clients.Clear();
        }

        #endregion
    }
}
