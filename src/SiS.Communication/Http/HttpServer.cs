using SiS.Communication.Tcp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Represents Http server based on SocketAsyncEvent(IOCP)
    /// </summary>
    public class HttpServer
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Http.HttpServer
        /// </summary>
        public HttpServer()
        {
            _contentLengthLowMark = Encoding.ASCII.GetBytes("content-length:");
            _contentLengthUpMark = Encoding.ASCII.GetBytes("CONTENT-LENGTH:");
            _handlers = new ObservableCollection<HttpHandler>();
            _handlers.CollectionChanged += _handlers_CollectionChanged;
        }
        #endregion

        #region Private members
        private static readonly byte[] HeaderEndMark = { (byte)0x0d, (byte)0x0a, (byte)0x0d, (byte)0x0a };
        private readonly byte[] _contentLengthLowMark;
        private readonly byte[] _contentLengthUpMark;
        private HttpServerConfig _serverConfig;//server parameter
        private ushort _listenPort; //listen port
        private Socket _serverSocket; //server listen socket
        private bool _isRunning = false;
        private ILog _logger;
        private bool _isWebsocketEnabled = false;
        private ByteSegmentPool _httpBufferPool;
        #endregion

        #region Properties

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
        /// Gets a value indicating whether the server is running.
        /// </summary>
        /// <returns>true if the tcp is running; otherwise, false. The default is false.</returns>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        private ObservableCollection<HttpHandler> _handlers;
        /// <summary>
        /// Gets the collection of object that handle the request.User can add a custom handler to the collection.
        /// </summary>
        public ObservableCollection<HttpHandler> Handlers
        {
            get { return _handlers; }
        }

        #endregion

        #region Event Handlers

        private void _handlers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (IsRunning)
            {
                throw new Exception("Can not add handler when the server is running.");
            }
            if (e.NewItems != null)
            {
                foreach (HttpHandler hander in e.NewItems)
                {
                    if (hander is WebsocketHandler)
                    {
                        _isWebsocketEnabled = true;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Tcp connection
        private HttpClientContext AddNewClient(Socket newClient)
        {
            HttpClientContext context = new HttpClientContext();
            SocketAsyncEventArgs sockEventArgs = new SocketAsyncEventArgs();
            sockEventArgs.AcceptSocket = newClient;

            if (_httpBufferPool != null)
            {
                ArraySegment<byte> buffer = _httpBufferPool.GetBuffer();
                sockEventArgs.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
            }
            else
            {
                byte[] buffer = new byte[_serverConfig.TcpConfig.SocketAsyncBufferSize];
                sockEventArgs.SetBuffer(buffer, 0, buffer.Length);
            }

            sockEventArgs.UserToken = context;
            context.ClientID = (long)newClient.Handle;
            if (_serverConfig.TcpConfig.ReceiveDataMaxSpeed != TcpConfig.NotLimited)
            {
                context.RecvSpeedController.LimitSpeed = _serverConfig.TcpConfig.ReceiveDataMaxSpeed;
                context.RecvSpeedController.Enabled = true;
            }
            if (_serverConfig.TcpConfig.SendDataMaxSpeed != TcpConfig.NotLimited)
            {
                context.SendController.LimitSpeed = _serverConfig.TcpConfig.SendDataMaxSpeed;
                context.SendController.Enabled = true;
            }
            sockEventArgs.Completed += SockAsyncArgs_Completed;
            IPEndPoint remoteIPEnd = (IPEndPoint)newClient.RemoteEndPoint;
            //save original ip end point, so we can always get the remote ip end point even the socket is closed.
            context.IPEndPoint = new IPEndPoint(remoteIPEnd.Address, remoteIPEnd.Port);
            if (!newClient.ReceiveAsync(sockEventArgs))
            {
                ProcessReceive(sockEventArgs);
            }
            return context;
        }

        private void SockAsyncArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                ProcessReceive(e);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs sockAsyncArgs)
        {
            if (!_isRunning)
            {
                if (sockAsyncArgs.UserToken != null)
                {
                    CloseClient(sockAsyncArgs);
                }
                return;
            }
            HttpClientContext clientContext = sockAsyncArgs.UserToken as HttpClientContext;
            Socket sockClient = sockAsyncArgs.AcceptSocket;
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
                    int endPos = 0;
                    try
                    {
                        //messageList = _packetSpliter.GetPackets(clientRingBuffer.Buffer, 0, clientRingBuffer.DataLength, clientID, out endPos);
                        if (clientContext.IsWebSocket)
                        {
                            List<WebSocketPacket> webSockPackets = GetWebsocketPackets(clientRingBuffer.Buffer, 0, clientRingBuffer.DataLength, out endPos);
                            if (webSockPackets != null)
                            {
                                try
                                {
                                    clientRingBuffer.Remove(endPos);
                                    foreach (WebSocketPacket wsPacket in webSockPackets)
                                    {
                                        if (wsPacket.DataType == WSPacketType.Disconnect)
                                        {
                                            CloseClient(sockAsyncArgs);
                                            break;
                                        }
                                        else
                                        {
                                            WebSocketDataReceived?.Invoke(this, new WebSocketDataReceivedEventArgs()
                                            {
                                                ClientID = clientContext.ClientID,
                                                DataPacket = wsPacket,
                                                RemoteEndPoint = clientContext.IPEndPoint
                                            });
                                        }
                                    }
                                }
                                catch (InvalidPacketException)
                                {
                                    //invalid data received , indicates the client has made a illegal connection, we should disconnect it.
                                    _logger?.Info("illegal connection detected");
                                    if (_isRunning)
                                        CloseClient(sockAsyncArgs);
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    _logger?.Warn("an error has occurred during get packets", ex.Message);
                                }
                            }

                        }
                        else
                        {
                            List<HttpPacket> httpPackets = GetHttpPackets(clientRingBuffer.Buffer, 0, clientRingBuffer.DataLength, clientContext, out endPos);
                            if (httpPackets != null)
                            {
                                try
                                {
                                    clientRingBuffer.Remove(endPos);
                                    foreach (HttpPacket httpPacket in httpPackets)
                                    {
                                        ProcessHttpPacket(httpPacket, sockAsyncArgs);
                                    }
                                }
                                catch (InvalidPacketException)
                                {
                                    //invalid data received , indicates the client has made a illegal connection, we should disconnect it.
                                    _logger?.Info("illegal connection detected");
                                    if (_isRunning)
                                        CloseClient(sockAsyncArgs);
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    _logger?.Warn("an error has occurred during get packets", ex.Message);
                                }
                            }
                        }
                    }
                    catch
                    {
                        CloseClient(sockAsyncArgs);
                        return;
                    }


                    //in case of the socket is closed, the following statements may cause of exception, so we should use try catch
                    try
                    {
                        if (!sockClient.ReceiveAsync(sockAsyncArgs))
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
                        CloseClient(sockAsyncArgs);
                }
            }
            else
            {
                if (_isRunning)
                    CloseClient(sockAsyncArgs);
            }
        }

        private void CloseClient(SocketAsyncEventArgs sockAsyncEventArgs)
        {
            if (sockAsyncEventArgs.UserToken != null)
            {
                HttpClientContext clientContext = sockAsyncEventArgs.UserToken as HttpClientContext;
                sockAsyncEventArgs.AcceptSocket.Close();
                sockAsyncEventArgs.AcceptSocket.Dispose();
                if (_httpBufferPool != null)
                {
                    _httpBufferPool.RecycleBuffer(new ArraySegment<byte>(sockAsyncEventArgs.Buffer, sockAsyncEventArgs.Offset, sockAsyncEventArgs.Count));
                }
                sockAsyncEventArgs.Dispose();
                sockAsyncEventArgs.UserToken = null;

                if (clientContext.IsWebSocket)
                {
                    WebSocketStatusChanged?.Invoke(this, new WebSocketStatusChangedEventArgs()
                    {
                        Client = new WebSocketClientContext()
                        {
                            ClientID = clientContext.ClientID
                        }
                    });
                }
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
                    AddNewClient(clientSocket);
                }
            }
            if (sockAsyncEventArgs.SocketError != SocketError.OperationAborted)
            {
                StartAccept(sockAsyncEventArgs);
            }
        }

        private void StartAccept(SocketAsyncEventArgs listenSockAsyncEventArgs)
        {
            if (!_isRunning)
            {
                return;
            }
            if (listenSockAsyncEventArgs == null)
            {
                listenSockAsyncEventArgs = new SocketAsyncEventArgs();
                listenSockAsyncEventArgs.Completed += (sender, sockAsyncArgs) =>
                {
                    ProcessAccept(sockAsyncArgs);
                };
            }
            else
            {
                //socket must be cleared since the context object is being reused
                listenSockAsyncEventArgs.AcceptSocket = null;
            }
            try
            {
                if (!_serverSocket.AcceptAsync(listenSockAsyncEventArgs))
                {
                    ProcessAccept(listenSockAsyncEventArgs);
                }
            }
            catch
            {
                return;
            }
        }

        private void SendData(byte[] buffer, int offset, int count, Socket socketClient)
        {
            int sendTotal = 0;
            int sendIndex = offset;
            while (sendTotal < count)
            {
                int single = socketClient.Send(buffer, sendIndex, count - sendTotal, SocketFlags.None);
                sendTotal += single;
                sendIndex += single;
            }
        }
        private void SendData(byte[] buffer, Socket socketClient)
        {
            SendData(buffer, 0, buffer.Length, socketClient);
        }
        #endregion

        #region Events

        /// <summary>
        /// Represents the method that will handle the UnhandledRequestReceived event of a SiS.Communication.Http.HttpServer object.
        /// </summary>
        public event UnhandledRequestReceivedEventHandler UnhandledRequestReceived;

        /// <summary>
        /// Represents the method that will handle the WebSocketDataReceived event of a SiS.Communication.Http.HttpServer object.
        /// </summary>
        public event WebSocketDataReceivedEventHandler WebSocketDataReceived;

        /// <summary>
        /// Represents the method that will handle the WebSocketStatusChanged event of a SiS.Communication.Http.HttpServer object.
        /// </summary>
        public event WebSocketStatusChangedEventHandler WebSocketStatusChanged;
        #endregion

        #region Public functions


        /// <summary>
        /// Start tcp server listening on a specific port.
        /// </summary>
        /// <param name="listenPort">The listening port of the server.</param>
        public void Start(ushort listenPort)
        {
            Start(listenPort, new HttpServerConfig());
        }

        /// <summary>
        /// Start udp server listening on a specific port using TcpServerParam parameter.
        /// </summary>
        /// <param name="listenPort">The listening port of the server.</param>
        /// <param name="serverConfig">The server's parameter, see TcpServerParam.</param>
        public void Start(ushort listenPort, HttpServerConfig serverConfig)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException(Constants.ExMessageServerAlreadyRunning);
            }
            // _clientContextPool = new ClientContextPool(serverConfig.MaxClientCount, serverConfig.SocketAsyncBufferSize);
            _httpBufferPool = new ByteSegmentPool(serverConfig.TcpConfig.MaxClientCount, serverConfig.TcpConfig.SocketAsyncBufferSize);
            Contract.Requires(listenPort > 0 && listenPort < 65535);
            Contract.Requires(serverConfig != null);
            _serverConfig = serverConfig;
            _listenPort = listenPort;
            try
            {
                //create listen socket
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (_serverConfig.TcpConfig.EnableKeepAlive)
                {
                    TcpUtility.SetKeepAlive(_serverSocket, _serverConfig.TcpConfig.KeepAliveTime, _serverConfig.TcpConfig.KeepAliveInterval);
                }
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, listenPort));
                _serverSocket.Listen(_serverConfig.TcpConfig.MaxPendingCount);
            }
            catch (Exception ex)
            {
                throw new Exception(Constants.ExMessageStartServerFailed, ex);
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
            _httpBufferPool = null;
        }

        #endregion

        #region Http
        private int SearchByteArray(byte[] desArray, int offset, int count, byte[] searchArray)
        {
            count = Math.Min(count, desArray.Length - offset);
            if (searchArray.Length > count)
            {
                return -1;
            }
            bool firstLineEndFound = false;
            int maxIndex = count + offset - searchArray.Length;
            for (int i = offset; i <= maxIndex; i++)
            {
                if (i > _serverConfig.MaxHeaderLength)
                {
                    throw new InvalidPacketException($"The length of the http header has exceed {_serverConfig.MaxHeaderLength}.");
                }

                if (!firstLineEndFound)
                {
                    if (i > _serverConfig.MaxUrlLength + 20)
                    {
                        throw new InvalidPacketException($"The length of the url can not exceed {_serverConfig.MaxUrlLength}");
                    }
                    if (i < maxIndex && searchArray[i] == '\r' && searchArray[i + 1] == '\n')
                    {
                        firstLineEndFound = true;
                    }
                }
                bool bFound = true;
                for (int j = 0; j < searchArray.Length; j++)
                {
                    if (searchArray[j] != desArray[i + j])
                    {
                        bFound = false;
                        break;
                    }
                }
                if (bFound)
                {
                    return i;
                }
            }
            return -1;
        }
        private int SearchByteArrayNoCase(byte[] desArray, int offset, int count, byte[] searchLowArray, byte[] searchUpArray)
        {
            if (searchLowArray.Length != searchUpArray.Length)
            {
                return -1;
            }
            count = Math.Min(count, desArray.Length - offset);
            if (searchLowArray.Length > count)
            {
                return -1;
            }
            for (int i = offset; i <= count + offset - searchLowArray.Length; i++)
            {
                bool bFound = true;
                for (int j = 0; j < searchLowArray.Length; j++)
                {
                    byte val = desArray[i + j];
                    if (searchLowArray[j] != val && searchUpArray[j] != val)
                    {
                        bFound = false;
                        break;
                    }
                }
                if (bFound)
                {
                    return i;
                }
            }
            return -1;
        }
        private int FindHeaderEnd(byte[] buffer, int offset, int count)
        {
            return SearchByteArray(buffer, offset, count, HeaderEndMark);
        }
        private void ValidFirstLine(byte[] buffer, int offset, int count)
        {
            if (count < 10)
            {
                return;
            }
            bool isFound = true;
            for (int i = offset; i < offset + 10; i++)
            {
                if (buffer[i] == ' ')
                {
                    isFound = true;
                    break;
                }
            }
            if (!isFound)
            {
                throw new InvalidPacketException();
            }
        }
        private int GetContentLength(byte[] buffer, int offset, int count)
        {
            int result = 0;
            int startPos = -1, endPos = -1;
            bool found = false;
            for (int i = offset; i < count + offset; i++)
            {
                bool isNumber = buffer[i] >= '0' && buffer[i] <= '9';
                if (!found)
                {
                    if (isNumber)
                    {
                        found = true;
                        startPos = i;
                    }
                }
                else
                {
                    if (!isNumber)
                    {
                        endPos = i - 1;
                        break;
                    }
                }
            }
            if (startPos == -1 || endPos == -1 || startPos > endPos)
            {
                return 0;
            }
            for (int i = endPos, pow = 1; i >= startPos; i--, pow *= 10)
            {
                result += (int)(buffer[i] - '0') * pow;
            }
            return result;
        }
        private List<HttpPacket> GetHttpPackets(byte[] streamBuffer, int offset, int count, HttpClientContext context, out int endPos)
        {
            List<HttpPacket> dataPacketList = null;
            //first, get large data session
            if (context.PacketType == HttpPacketType.Begin || context.PacketType == HttpPacketType.Bin)
            {
                int remainLength = context.RemainLength;
                int binDataLength = 0;
                bool isFinished = false;
                if (count < remainLength)
                {
                    binDataLength = count;
                }
                else
                {
                    binDataLength = remainLength;
                    isFinished = true;
                }

                endPos = offset + binDataLength;
                var binData = new ArraySegment<byte>(streamBuffer, offset, binDataLength);
                dataPacketList = new List<HttpPacket>();
                dataPacketList.Add(
                    new HttpPacket()
                    {
                        PacketType = isFinished ? HttpPacketType.End : HttpPacketType.Bin,
                        BodyData = binData,
                        BodyTotalLength = context.ContentLength
                    });
                if (isFinished)
                {
                    context.PacketType = HttpPacketType.End;
                }
                else
                {
                    context.PacketType = HttpPacketType.Bin;
                    context.LastActiveTime = DateTime.Now;
                    context.FinishedLength += binDataLength;
                }
                return dataPacketList;
            }

            endPos = 0;
            while (true)
            {
                ValidFirstLine(streamBuffer, offset, count);
                int headerEndPos = FindHeaderEnd(streamBuffer, offset, count);
                if (headerEndPos == -1)
                {
                    break;
                }
                headerEndPos += HeaderEndMark.Length;
                int headerLen = headerEndPos - offset;
                //search "Content-Length" in header
                int contentLen = 0;
                int contentLenPos = SearchByteArrayNoCase(streamBuffer, offset, headerLen, _contentLengthLowMark, _contentLengthUpMark);
                if (contentLenPos >= 0)
                {
                    contentLenPos += _contentLengthLowMark.Length;
                    contentLen = GetContentLength(streamBuffer, contentLenPos, count - contentLenPos);
                }

                bool isLargeData = contentLen > _serverConfig.MaxBodyCache;
                var headerData = new ArraySegment<byte>(streamBuffer, offset, headerLen);
                if (contentLen == 0 || isLargeData)
                {
                    if (dataPacketList == null)
                    {
                        dataPacketList = new List<HttpPacket>();
                    }

                    dataPacketList.Add(new HttpPacket()
                    {
                        PacketType = isLargeData ? HttpPacketType.Begin : HttpPacketType.Complete,
                        HeaderData = headerData,
                        BodyTotalLength = isLargeData ? contentLen : 0
                    });
                    offset += headerLen;
                    count -= headerLen;
                    endPos = headerEndPos;
                    if (isLargeData)
                    {
                        //create large data session and save
                        context.ContentLength = contentLen;
                        context.FinishedLength = 0;
                        context.LastActiveTime = DateTime.Now;
                        context.PacketType = HttpPacketType.Begin;
                        return dataPacketList;
                    }
                    else
                    {
                        context.PacketType = HttpPacketType.Complete;
                        continue;
                    }
                }

                //If the data is not complete, return
                if (headerLen + contentLen > count)
                {
                    return dataPacketList;
                }

                if (dataPacketList == null)
                {
                    dataPacketList = new List<HttpPacket>();
                }
                context.PacketType = HttpPacketType.Complete;
                dataPacketList.Add(new HttpPacket()
                {
                    PacketType = HttpPacketType.Complete,
                    HeaderData = headerData,
                    BodyData = new ArraySegment<byte>(streamBuffer, offset + headerLen, contentLen)
                });
                offset += (headerLen + contentLen);
                count -= (headerLen + contentLen);
                endPos = offset;
            }
            return dataPacketList;
        }
        private void SendResponse(SocketAsyncEventArgs eventArgs, HttpResponseMessage responseMsg)
        {
            string header = $"HTTP/1.1 {(int)responseMsg.StatusCode} {responseMsg.StatusCode.ToString()}\r\n";

            header += responseMsg.Headers.ToString();
            if (responseMsg.Content != null)
            {
                header += responseMsg.Content.Headers.ToString();
            }
            if (responseMsg.Content == null)
            {
                header += "Content-Length: 0\r\n";
            }
            header += "\r\n";

            SendData(Encoding.ASCII.GetBytes(header), eventArgs.AcceptSocket);
            if (responseMsg.Content != null)
            {
                if (responseMsg.Content is StreamContent)
                {
                    StreamContent streamContent = responseMsg.Content as StreamContent;
                    Stream stream = streamContent.ReadAsStreamAsync().Result;
                    byte[] buffer = new byte[100 * 1024];
                    try
                    {
                        long readTotal = 0;
                        while (readTotal < stream.Length)
                        {
                            int readLen = stream.Read(buffer, 0, buffer.Length);
                            readTotal += readLen;
                            SendData(buffer, 0, buffer.Length, eventArgs.AcceptSocket);
                        }
                    }
                    catch
                    {
                    }
                    stream.Close();
                    stream.Dispose();
                }
                else
                {
                    byte[] body = responseMsg.Content.ReadAsByteArrayAsync().Result;
                    try
                    {
                        SendData(body, eventArgs.AcceptSocket);
                    }
                    catch { }
                }
            }
        }

        private HttpRequestMessage ParseToRequestMessage(HttpPacket httpPacket, SocketAsyncEventArgs eventArgs)
        {
            //header
            ArraySegment<byte> headerData = httpPacket.HeaderData.Value;
            string strHeaderText = Encoding.ASCII.GetString(headerData.Array, headerData.Offset, headerData.Count);
            strHeaderText = strHeaderText.Trim();
            StringReader reader = new StringReader(strHeaderText);
            string strLine = reader.ReadLine();
            if (strLine == null)
            {
                throw new Exception("invalid protocol");
            }
            //Parse first line
            int firstSpacePos = strLine.IndexOf(" ");
            int lastSpacePos = strLine.LastIndexOf(" ");

            if (firstSpacePos == -1 || lastSpacePos == -1 || firstSpacePos == lastSpacePos)
            {
                throw new Exception("invalid protocol");
            }
            //if the length of the url exceeds MaxUrlLength, return null;
            if (lastSpacePos - firstSpacePos > _serverConfig.MaxUrlLength)
            {
                return null;
            }
            string method = strLine.Substring(0, firstSpacePos).Trim();
            string url = strLine.Substring(firstSpacePos + 1, lastSpacePos - firstSpacePos - 1).Trim();
            string version = strLine.Substring(lastSpacePos + 1).Trim().ToUpper();
            if (version != "HTTP/1.0" && version != "HTTP/1.1")
            {
                throw new Exception("invalid protocol");
            }
            HttpRequestMessage requestMsg = new HttpRequestMessage(new HttpMethod(method), url);
            HttpContent httpContent = null;

            if (httpPacket.PacketType == HttpPacketType.Complete && httpPacket.BodyData != null)
            {
                httpContent = new ByteArrayContent(httpPacket.BodyData.Value.Array, httpPacket.BodyData.Value.Offset, httpPacket.BodyData.Value.Count);
            }
            else if (httpPacket.PacketType == HttpPacketType.Begin)
            {
                BlockStream stream = new BlockStream(httpPacket.BodyTotalLength, 50 * 1024, 8 * 1024 * 1024);
                stream.ReadTimeout = _serverConfig.RequestTimeout;
                httpContent = new BlockStreamContent(stream);
                HttpClientContext context = eventArgs.UserToken as HttpClientContext;
                context.LargeDataStream = stream;
            }

            requestMsg.Version = Version.Parse(version.Substring(5));
            while (true)
            {
                strLine = reader.ReadLine();
                if (strLine == null)
                {
                    break;
                }
                strLine = strLine.Trim();
                if (strLine == "")
                {
                    continue;
                }
                int firstPos = strLine.IndexOf(":");
                if (firstPos == -1 || firstPos == strLine.Length - 1 || firstPos == 0)
                {
                    return null;
                }

                string key = strLine.Substring(0, firstPos).Trim();
                string value = strLine.Substring(firstPos + 1).Trim();
                if (key == "" || value == "")
                {
                    return null;
                }
                if (!key.ToLower().StartsWith("content-"))
                {
                    requestMsg.Headers.Add(key, value);
                }
                else if (httpContent != null)
                {
                    httpContent.Headers.Add(key, value);
                }
            }

            requestMsg.Content = httpContent;
            return requestMsg;
        }
        private void ProcessHttpPacket(HttpPacket httpPacket, SocketAsyncEventArgs eventArgs)
        {
            if (httpPacket.PacketType == HttpPacketType.Complete)
            {
                ProcessCompleteRequest(httpPacket, eventArgs);
            }
            else if (httpPacket.PacketType == HttpPacketType.Begin)
            {
                HttpRequestMessage requestMsg = null;
                try
                {
                    requestMsg = ParseToRequestMessage(httpPacket, eventArgs);
                }
                catch
                {
                    //Invalid http protocol , it may be an attack, so close the connection.
                    CloseClient(eventArgs);
                    return;
                }
                //bad request
                if (requestMsg == null)
                {
                    HttpResponseMessage repMsg = ResponseMsgHelper.CreateSimpleRepMsg();
                    repMsg.StatusCode = HttpStatusCode.BadRequest;
                    SendResponse(eventArgs, repMsg);
                    return;
                }

                HttpClientContext clientContext = eventArgs.UserToken as HttpClientContext;
                HttpContext httpCtx = new HttpContext()
                {
                    Request = requestMsg,
                    RemoteEndPoint = clientContext.IPEndPoint
                };
                clientContext.LargeDataHttpContext = httpCtx;
                clientContext.LargeDataHandleTask = Task.Factory.StartNew((obj) =>
                {
                    UnhandledRequestReceived?.Invoke(this, new UnhandledRequestReceivedEventArgs()
                    {
                        Context = (HttpContext)obj,
                        ClientID = clientContext.ClientID
                    });
                }, httpCtx);
            }
            else if (httpPacket.PacketType == HttpPacketType.Bin || httpPacket.PacketType == HttpPacketType.End)
            {
                HttpClientContext clientContext = eventArgs.UserToken as HttpClientContext;
                BlockStream stream = clientContext.LargeDataStream;
                bool isInvalidClient = true;
                do
                {
                    if (stream == null || httpPacket.BodyData == null || clientContext.LargeDataHandleTask == null
                        || clientContext.LargeDataHttpContext == null)
                    {
                        break;
                    }
                    try
                    {
                        stream.Write(httpPacket.BodyData.Value.Array, httpPacket.BodyData.Value.Offset, httpPacket.BodyData.Value.Count);
                    }
                    catch
                    {
                        break;
                    }

                    if (httpPacket.PacketType == HttpPacketType.End)
                    {
                        try
                        {
                            stream.Flush();
                            if (!clientContext.LargeDataHandleTask.Wait(5000))
                            {
                                break;
                            }
                            isInvalidClient = false;
                            if (clientContext.LargeDataHttpContext.Response != null)
                            {
                                SendResponse(eventArgs, clientContext.LargeDataHttpContext.Response);
                            }
                            else
                            {
                                HttpResponseMessage repMsg = ResponseMsgHelper.CreateSimpleRepMsg();
                                repMsg.StatusCode = HttpStatusCode.NotFound;
                                SendResponse(eventArgs, repMsg);
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                    else
                    {
                        isInvalidClient = false;
                    }
                }
                while (false);

                if(isInvalidClient || httpPacket.PacketType == HttpPacketType.End)
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    clientContext.LargeDataStream = null;
                    clientContext.LargeDataHttpContext = null;
                    clientContext.LargeDataHandleTask = null;
                    if (isInvalidClient)
                    {
                        CloseClient(eventArgs);
                        return;
                    }
                }
            }
        }

        private void ProcessCompleteRequest(HttpPacket httpPacket, SocketAsyncEventArgs eventArgs)
        {
            HttpResponseMessage repMsg = ResponseMsgHelper.CreateSimpleRepMsg();
            repMsg.StatusCode = HttpStatusCode.NotFound;
            HttpRequestMessage requestMsg = null;
            do
            {
                try
                {
                    requestMsg = ParseToRequestMessage(httpPacket, eventArgs);
                }
                catch
                {
                    //Invalid http protocol , it may be an attack, so close the connection.
                    CloseClient(eventArgs);
                    return;
                }
                if (requestMsg == null)
                {
                    repMsg.StatusCode = HttpStatusCode.BadRequest;
                    break;
                }
                HttpClientContext clientContext = eventArgs.UserToken as HttpClientContext;
                HttpContext context = new HttpContext()
                {
                    Request = requestMsg,
                    RemoteEndPoint = clientContext.IPEndPoint
                };
                //first, use internal handlers
                bool isHandled = false;
                foreach (HttpHandler handler in _handlers)
                {
                    try
                    {
                        handler.Process(context);
                        if (context.Response != null)
                        {
                            repMsg = context.Response;
                            //switch to websocket
                            if (handler is WebsocketHandler)
                            {
                                clientContext.IsWebSocket = true;
                                WebSocketStatusChanged?.Invoke(this, new WebSocketStatusChangedEventArgs()
                                {
                                    Client = new WebSocketClientContext()
                                    {
                                        ClientID = clientContext.ClientID,
                                        ClientArgs = eventArgs
                                    }
                                });
                            }
                            isHandled = true;
                            break;
                        }
                    }
                    catch
                    {
                        repMsg.StatusCode = HttpStatusCode.InternalServerError;
                        isHandled = true;
                        break;
                    }
                }
                if (isHandled)
                {
                    break;
                }
                try
                {
                    //then, use event handler
                    UnhandledRequestReceived?.Invoke(this, new UnhandledRequestReceivedEventArgs()
                    {
                        Context = context
                    });
                }
                catch
                {
                    repMsg.StatusCode = HttpStatusCode.InternalServerError;
                    isHandled = true;
                    break;
                }
                if (context.Response != null)
                {
                    repMsg = context.Response;
                }

            } while (false);
            if (repMsg.RequestMessage == null)
            {
                repMsg.RequestMessage = requestMsg;
            }
            SendResponse(eventArgs, repMsg);
        }
        #endregion

        #region WebSocket
        private void DecryptFrameDataWithMask(WebSocketFrame dataFrame)
        {
            if (dataFrame.HasMask)
            {
                for (int i = 0; i < (int)dataFrame.BodyData.Count; i++)
                {
                    dataFrame.BodyData.Array[dataFrame.BodyData.Offset + i] = (byte)(dataFrame.BodyData.Array[dataFrame.BodyData.Offset + i] ^ dataFrame.MaskData.Array[dataFrame.MaskData.Offset + i % 4]);
                }
            }
        }
        private WebSocketFrame TryGetWSFrame(byte[] streamBuffer, int offset, int count, out int endPos)
        {
            endPos = 0;
            int index = offset;

            if (count < 4)
            {
                return null;
            }

            WebSocketFrame wsFrame = new WebSocketFrame();
            wsFrame.IsEof = (streamBuffer[index] >> 7) > 0;
            wsFrame.DataType = (WSPacketType)(streamBuffer[index] & 0xF);
            if (wsFrame.DataType != WSPacketType.Bin
                && wsFrame.DataType != WSPacketType.Disconnect
                && wsFrame.DataType != WSPacketType.Frame
                && wsFrame.DataType != WSPacketType.Ping
                && wsFrame.DataType != WSPacketType.Pang
                && wsFrame.DataType != WSPacketType.Text)
            {
                throw new InvalidPacketException();
            }
            index++;
            wsFrame.HasMask = (streamBuffer[index] >> 7) > 0;
            if (!wsFrame.HasMask)
            {
                throw new InvalidPacketException();
            }
            int packPrevLen = streamBuffer[index] & 0x7F;
            index++;
            long payloadLen = 0;
            if (packPrevLen < 126)
            {
                payloadLen = packPrevLen;
            }
            else if (packPrevLen == 126)
            {
                //UInt16 networkLen = BitConverter.ToUInt16(streamBuffer, index);
                payloadLen = streamBuffer[index] * 256 + streamBuffer[index + 1];
                index += 2;
            }
            else if (packPrevLen == 127)
            {
                if (count < index + 8)
                {
                    return null;
                }
                payloadLen = BitConverter.ToInt64(streamBuffer, index);
                payloadLen = IPAddress.NetworkToHostOrder(payloadLen);
                index += 8;
            }
            if (payloadLen > _serverConfig.WSMaxPacketLength)
            {
                throw new InvalidPacketException($"the packet length has exceed {_serverConfig.WSMaxPacketLength}");
            }
            if (wsFrame.HasMask)
            {
                if (count < index + 4)
                {
                    return null;
                }
                wsFrame.MaskData = new ArraySegment<byte>(streamBuffer, index, 4);
                index += 4;
            }
            if (index + (int)payloadLen > count)
            {
                return null;
            }

            if (payloadLen > 0)
            {
                wsFrame.BodyData = new ArraySegment<byte>(streamBuffer, index, (int)payloadLen);
            }

            index += (int)payloadLen;
            wsFrame.FrameData = new ArraySegment<byte>(streamBuffer, offset, index - offset);
            endPos = index;
            return wsFrame;
        }
        private List<WebSocketPacket> GetWebsocketPackets(byte[] streamBuffer, int offset, int count, out int endPos)
        {
            List<WebSocketPacket> dataPacketList = null;
            //endPos = 0;
            //int index = offset;
            endPos = 0;
            List<WebSocketFrame> frameGroup = null;
            while (true)
            {
                int newEndPos = 0;
                WebSocketFrame frame = TryGetWSFrame(streamBuffer, offset, count, out newEndPos);
                if (frame != null)
                {
                    offset += frame.FrameData.Count;
                    count -= frame.FrameData.Count;
                    if (frame.DataType == WSPacketType.Disconnect || frame.DataType == WSPacketType.Ping)
                    {
                        if (frameGroup != null && frameGroup.Count >= 1)
                        {
                            continue;
                        }
                    }
                    if (frameGroup == null)
                    {
                        frameGroup = new List<WebSocketFrame>();
                    }

                    frameGroup.Add(frame);
                }
                else
                {
                    //if (frameGroup == null)
                    //{
                    return dataPacketList;
                    // }
                }

                //transform group to packet
                if (frame.IsEof)
                {
                    endPos = newEndPos;
                    if (frameGroup.Count == 1)
                    {
                        DecryptFrameDataWithMask(frame);
                        WebSocketPacket dataPacket = new WebSocketPacket()
                        {
                            DataType = frame.DataType,
                            Data = frame.BodyData
                        };
                        if (dataPacketList == null)
                        {
                            dataPacketList = new List<WebSocketPacket>();
                        }
                        dataPacketList.Add(dataPacket);
                        frameGroup = null;
                        continue;
                    }

                    foreach (WebSocketFrame dataFrame in frameGroup)
                    {
                        DecryptFrameDataWithMask(dataFrame);
                    }
                    List<WebSocketPacket> packets = WebSocketPacket.Convert(frameGroup);
                    foreach (WebSocketPacket wsPacket in packets)
                    {
                        dataPacketList.Add(wsPacket);
                    }
                }
            }
        }
        private ArraySegment<byte> WSMakePacket(byte[] data, int offset, int count, WSPacketSendType sendType, DynamicBufferStream sendBuffer)
        {
            if (count > _serverConfig.WSMaxPacketLength)
            {
                throw new InvalidPacketException($"the packet length has exceed {_serverConfig.WSMaxPacketLength}");
            }

            sendBuffer.SetLength(count + 16);
            int index = 0;
            byte b1 = 0x80;
            b1 |= (byte)sendType;
            sendBuffer.Buffer[index] = b1;
            index++;
            byte b2 = 0x0;
            if (count < 126)
            {
                b2 += (byte)count;
                sendBuffer.Buffer[index] = b2;
                index++;
            }
            else if (count >= 126 && count <= UInt16.MaxValue)
            {
                b2 += 126;
                sendBuffer.Buffer[index] = b2;
                index++;
                int lenHigh = count / 256;
                int lenLow = count % 256;
                sendBuffer.Buffer[index] = (byte)lenHigh;
                sendBuffer.Buffer[index + 1] = (byte)lenLow;
                index += 2;
            }
            else
            {
                b2 += 127;
                sendBuffer.Buffer[index] = b2;
                index++;
                long len = count;
                byte[] lenBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len));
                Array.Copy(lenBytes, 0, sendBuffer.Buffer, index, 8);
                index += 8;
            }
            Array.Copy(data, 0, sendBuffer.Buffer, index, count);
            index += count;
            return new ArraySegment<byte>(sendBuffer.Buffer, 0, index);
        }

        #endregion

        #region Websocket sending

        /// <summary>
        /// Sends the specified number of bytes of data to a connected web socket client
        ///     starting at the specified offset.
        /// </summary>
        /// <param name="client">The client context to received the message.</param>
        /// <param name="data">An array of type System.Byte that contains the data to be sent.</param>
        /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
        /// <param name="count">The number of bytes to send.</param>
        /// <param name="type">The message type of the web socket packet.</param>
        public void WebSocketSend(IClientContext client, byte[] data, int offset, int count, WSPacketSendType type)
        {
            if (!_isRunning)
            {
                throw new Exception(Constants.ExMessageServerNotRunning);
            }
            if (!client.IsConnected)
            {
                throw new Exception("The websocket has disconnected.");
            }
            if (!_isWebsocketEnabled)
            {
                throw new Exception("The websocket handler is required.");
            }
            SocketAsyncEventArgs clientArgs = (client as WebSocketClientContext).ClientArgs;
            HttpClientContext httpClientContext = clientArgs.UserToken as HttpClientContext;
            DynamicBufferStream sendBuffer = null;
            sendBuffer = httpClientContext.SendBuffer;
            ArraySegment<byte> toSendData = WSMakePacket(data, offset, count, type, sendBuffer);
            SendData(toSendData.Array, toSendData.Offset, toSendData.Count, clientArgs.AcceptSocket);
        }

        /// <summary>
        /// Sends the specified number of bytes of data to a connected web socket client.
        /// </summary>
        /// <param name="client">The client context to received the message.</param>
        /// <param name="data">An array of type System.Byte that contains the data to be sent.</param>
        /// <param name="type">The message type of the web socket packet.</param>
        public void WebSocketSend(IClientContext client, byte[] data, WSPacketSendType type)
        {
            WebSocketSend(client, data, 0, data.Length, type);
        }

        /// <summary>
        /// Sends the specified data to a connected web socket client.
        /// </summary>
        /// <param name="client">The client context to received the message.</param>
        /// <param name="data">An array of type System.Byte that contains the data to be sent.</param>
        public void WebSocketSend(IClientContext client, byte[] data)
        {
            WebSocketSend(client, data, 0, data.Length, WSPacketSendType.Bin);
        }

        /// <summary>
        /// Sends the specified number of bytes of data to a connected web socket client
        ///     starting at the specified offset.
        /// </summary>
        /// <param name="client">The client context to received the message.</param>
        /// <param name="data">An array of type System.Byte that contains the data to be sent.</param>
        /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
        /// <param name="count">The number of bytes to send.</param>
        public void WebSocketSend(IClientContext client, byte[] data, int offset, int count)
        {
            WebSocketSend(client, data, offset, count, WSPacketSendType.Bin);
        }

        /// <summary>
        /// Sends the text to a connected web socket client.
        /// </summary>
        /// <param name="client">The client context to received the message.</param>
        /// <param name="text">The text to be sent.</param>
        public void WebSocketSendText(IClientContext client, string text)
        {
            if (!_isWebsocketEnabled)
            {
                throw new Exception("The websocket handler is required.");
            }
            byte[] data = Encoding.UTF8.GetBytes(text);
            WebSocketSend(client, data, WSPacketSendType.Text);
        }

        /// <summary>
        /// Close a web socket client.
        /// </summary>
        /// <param name="client">The client context to be closed.</param>
        public void WebSocketCloseClient(IClientContext client)
        {
            try
            {
                WebSocketClientContext userClientCtx = client as WebSocketClientContext;
                if (userClientCtx != null && userClientCtx.ClientArgs != null)
                {
                    CloseClient(userClientCtx.ClientArgs);
                }
            }
            catch { }
        }
        #endregion
    }
}
