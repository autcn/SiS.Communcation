using SiS.Communication.Spliter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

        protected virtual void OnClientStatusChanged(bool isInThread, ClientStatusChangedEventArgs args, HashSet<string> clientGroups)
        {
        }

        protected virtual void OnRawMessageReceived(TcpRawMessageReceivedEventArgs args)
        {
        }
        protected virtual bool ReceivedMessageFilter(TcpRawMessageReceivedEventArgs tcpRawMessageArgs)
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

            if (!_clients.TryGetValue(clientID, out ClientContext clientContext))
            {
                return;
            }
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
                                OnRawMessageReceived(rawMessage);
                            }
                        }
                    }
                    catch (InvalidPacketException)
                    {
                        //invalid data received , indicates the client has made a illegal connection, we should disconnect it.
                        _logger?.Info("illegal connection detected");
                        if (_isRunning)
                            CloseClient(true, (long)sockClient.Handle);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warn("an error has occurred during get packets", ex.Message);
                    }

                    //in case of the socket is closed, the following statements may cause of exception, so we should use try catch
                    try
                    {
                        if (!clientContext.ClientSocket.ReceiveAsync(sockAsyncArgs))
                        {
                            ProcessReceive(sockAsyncArgs);
                        }
                    }
                    catch { }
                }
                else
                {
                    _logger?.Warn($"sockAsyncArgs got an error: {sockAsyncArgs.SocketError.ToString()}");
                    if (_isRunning)
                        CloseClient(true, clientID);
                }
            }
            else
            {
                if (_isRunning)
                    CloseClient(true, clientID);
            }
        }

        private void SockAsyncArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                ProcessReceive(e);
            }
        }

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
            bool bOK = _clients.TryAdd((long)newClient.Handle, context);
            if (this is TcpClientEx)
            {
                System.Diagnostics.Trace.Assert(bOK, $"client:add new client failed:{this.GetHashCode()}   {newClient.Handle}");
            }
            else
            {
                System.Diagnostics.Trace.Assert(bOK, $"server:add new client failed:{this.GetHashCode()}   {newClient.Handle}");
            }

            context.SockAsyncArgs.UserToken = (long)newClient.Handle;
            ClientStatusChangedEventArgs eventArgs = new ClientStatusChangedEventArgs()
            {
                ClientID = context.ClientID,
                IPEndPoint = newClient.RemoteEndPoint as IPEndPoint,
                Status = ClientStatus.Connected
            };

            OnClientStatusChanged(false, eventArgs, null);
            if (!newClient.ReceiveAsync(context.SockAsyncArgs))
            {
                ProcessReceive(context.SockAsyncArgs);
            }
            return context;
        }

        protected void CloseClient(bool isInThread, long clientID)
        {
            if (_clients.TryRemove(clientID, out ClientContext clientContext))
            {
                IPEndPoint ipEndPt = clientContext.IPEndPoint;
                clientContext.Status = ClientStatus.Closed;
                clientContext.ClientSocket.Close();
                clientContext.ClientSocket.Dispose();
                clientContext.SockAsyncArgs.Completed -= SockAsyncArgs_Completed;
                ClientStatusChangedEventArgs eventArgs = new ClientStatusChangedEventArgs()
                {
                    ClientID = clientID,
                    IPEndPoint = ipEndPt,
                    Status = ClientStatus.Closed
                };
                OnClientStatusChanged(isInThread, eventArgs, clientContext.Groups);
                if (_clientContextPool != null)
                {
                    clientContext.Reset();
                    _clientContextPool.Push(clientContext);
                }
            }
        }

        #endregion
    }
}
