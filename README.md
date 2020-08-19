# SiS.Communcation
## Features
This project provides a variety of communication components for .NET, including TCP, UDP, HTTP, process communication. 
TCP communication provided in libraries is very powerful, efficient and easy to use.
It contains a lightweight HTTP server and supports websocket.
## TCP Usage
### 1.Basic Usage

Server:  
``` CSharp
using SiS.Communication;
using SiS.Communication.Tcp;
using System.Linq;
using System.Windows;

public partial class ServerWnd : Window
{
    public ServerWnd()
    {
        InitializeComponent();
        _tcpServer = new TcpServer();
        _tcpServer.ClientStatusChanged += _tcpServer_ClientStatusChanged;
        _tcpServer.MessageReceived += _tcpServer_MessageReceived;
    }

    private TcpServer _tcpServer;
    private long _clientID;

    private void _tcpServer_MessageReceived(object sender, TcpRawMessageReceivedEventArgs args)
    {
        _clientID = args.Message.ClientID;
        byte[] data = args.Message.MessageRawData.ToArray();
             
    }

    private void _tcpServer_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
    {
            
    }

    private void SendMessage(byte[] data)
    {
        _tcpServer.SendMessage(_clientID, data);
    }
    
    private void StartServer()
    {
        _tcpServer.Start(9999);
    }

    private void StopServer()
    {
        _tcpServer.Stop();
    }
}
```
Client:
``` CSharp
using SiS.Communication;
using SiS.Communication.Tcp;
using System;
using System.Linq;
using System.Windows;

public partial class ClientWnd : Window
{
    public ClientWnd()
    {
        InitializeComponent();

        //true: enable auto reconnect; fase: disable auto reconnect
        _tcpClientEx = new TcpClientEx(true);
        _tcpClientEx.ClientStatusChanged += _tcpClientEx_ClientStatusChanged;
        _tcpClientEx.MessageReceived += _tcpClientEx_MessageReceived;
    }
    private TcpClientEx _tcpClientEx;

    private void _tcpClientEx_MessageReceived(object sender, TcpRawMessageReceivedEventArgs args)
    {
        byte[] data = args.Message.MessageRawData.ToArray();

    }

    private void _tcpClientEx_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
    {
        if (args.Status == ClientStatus.Connected)
        {

        }
        else if (args.Status == ClientStatus.Connecting)
        {

        }
        else if (args.Status == ClientStatus.Closed)
        {

        }
    }

    private void SendMessage(byte[] data)
    {
        try
        {
            _tcpClientEx.SendMessage(data);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void Connect()
    {
        _tcpClientEx.Connect("127.0.0.1", 9999);
    }

    private void Disconnect()
    {
        _tcpClientEx.Close();
    }
}
```
### 2. Spliters
Spliters are used to split stream data into packets. There are four packet spliters built-in.  
**2.1 SimplePacketSpliter**

| 4 bytes| Length bytes |
|:-:|:-:|
| Length | Message Content |

The length is a 32-bit integer in host byte order. If network byte order is used,  please use the following code:
``` CSharp
            _tcpServer = new TcpServer(new SimplePacketSpliter(true));

            _tcpClientEx = new TcpClientEx(true, new SimplePacketSpliter(true));
```
<br>  

**2.2 HeaderPacketSpliter**  

The HeaderPacketSpliter is used to prevent illegal network data.

| 4 bytes | 4 bytes| Length bytes |
|:-:|:-:|:-:|
| Header Tag | Length | Message Content |

 The sample code:
``` CSharp
            _tcpServer = new TcpServer(new HeaderPacketSpliter(0xFFFF1111));

            _tcpClientEx = new TcpClientEx(true, new HeaderPacketSpliter(0xFFFF1111));
```
<br>  

**2.3 EndMarkPacketSpliter**  

EndMarkPacketSpliter is used when the packet end with specific mark.

||*||*||
|:-:|:-:|:-:|:-:|:-:|
| Message Content | **End Mark** | Message Content | **End Mark** | ... | ... |


The sample code:
``` CSharp
            _tcpServer = new TcpServer(new EndMarkPacketSpliter(false, (byte)'\x04'));
            _tcpServer = new TcpServer(new EndMarkPacketSpliter(false, "\n");
            _tcpServer = new TcpServer(new EndMarkPacketSpliter(false, "\r\n");

            _tcpClientEx = new TcpClientEx(true, new EndMarkPacketSpliter(false, (byte)'\x04'));
            _tcpClientEx = new TcpClientEx(true, new EndMarkPacketSpliter(false, "\n");
            _tcpClientEx = new TcpClientEx(true, new EndMarkPacketSpliter(false, "\r\n");
```
<br>  

**2.4 RawPacketSpliter**  

The RawPacketSpliter will not split data stream into packet. It transmits the received data directly.
If you don't want to split data into packets, the RawPacketSpliter is a good choice.

|  |
|:-:|
| Raw data stream ... |

 The sample code:
``` CSharp
            _tcpServer = new TcpServer(RawPacketSpliter.Default);

            _tcpClientEx = new TcpClientEx(true, RawPacketSpliter.Default);
```
<br>

**2.5 FriendlyPacketSpliter**

The FriendlyPacketSpliter is an abstract class which has implemented IPacketSpliter interface. It provides two simple abstract methods for user to override. 

For example, the packet structure as following:

| 1byte | 4bytes | Length Bytes |
| :---: | :----: | :----------: |
| 0x01  | Length |   Content    |

We implement a class that derived from FriendlyPacketSpliter :

``` csharp
public class CustomPacketSpliter : FriendlyPacketSpliter
{
    public override byte[] MakePacket(byte[] toSendData)
    {
        //| 0x01(1byte) | content-length(4bytes) |   content(nbytes) |
        byte[] sendingPacket = new byte[toSendData.Length + 5];
        sendingPacket[0] = 1;
        byte[] lengthBits = BitConverter.GetBytes(toSendData.Length);
        Buffer.BlockCopy(lengthBits, 0, sendingPacket, 1, 4);
        Buffer.BlockCopy(toSendData, 0, sendingPacket, 5, toSendData.Length);
        return sendingPacket;
    }

    public override int TryGetPacketSize(byte[] receivedData)
    {
        if (receivedData.Length >= 5)
        {
            //note: the length must be the length of entire package, include the 	header and content
            return BitConverter.ToInt32(receivedData, 1) + 5;
        }
        return -1;
    }
}
```

Notice that the FriendlyPacketSpliter will allocate , copy and release memory everytime. So if you are implementing a high concurrency program, you should consider whether to use it. In that case, it is recommended to derive directly from the IPacketSpliter interface.

### 3. Model Transmission
It is very easy to transfer models using this library.  

Step1:  Define a model serializer. For example:

``` CSharp
using Newtonsoft.Json;
using SiS.Communication.Business;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace SiS.Communication.Demo
{
    public class JsonSerializer : ISerializer
    {
        public static JsonSerializer Default { get; } = new JsonSerializer();
        public object Deserialize(Type type, string value)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        public string Serialize(object model)
        {
            return JsonConvert.SerializeObject(model);
        }
    }
}

```
Step 2:  Define an enumeration for message header type.
``` CSharp
    public enum MessageHeader : int
    {
        Model = 1,
        Other
    }
```

Step 3:  Initialize "TcpModelConverter" which is used for model convert. You must put the following code where run before using tcp model transmission.

``` c#
TcpModelConverter.Initialize(JsonSerializer.Default, typeRegister =>
{
    typeRegister.Register(Assembly.GetExecutingAssembly());
});
```



The server example:

``` CSharp
public class TcpModelServerViewModel : ServerBaseViewModel
{
    public TcpModelServerViewModel()
    {
        _tcpServer = new TcpModelServer((int)MessageHeader.Model);
        _tcpServer.ClientStatusChanged += OnTcpServer_ClientStatusChanged;
        _tcpServer.MessageReceived += OnTcpServer_MessageReceived;
    }

    protected TcpModelServer _tcpServer;
    protected const ushort _serverPort = 9999;
    protected long _serverClientID = -1;

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

    public override void ServerSend()
    {
        if (_serverClientID > 0)
        {
            if (!string.IsNullOrWhiteSpace(ServerSendText))
            {
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
        }
    }

    public override void StopServer()
    {
        _tcpServer.Stop();
    }
}
```
The client sample:
``` CSharp
public class TcpModelClientViewModel : ClientBaseViewModel
{
    public TcpModelClientViewModel()
    {
        _tcpClient = new TcpModelClient(true, (int)MessageHeader.Model);
        _tcpClient.ClientStatusChanged += OnTcpCient_ClientStatusChanged;
        _tcpClient.MessageReceived += OnTcpClient_PacketReceived;
    }

    protected TcpModelClient _tcpClient;
    private const ushort _serverPort = 9999;

    protected virtual void OnTcpCient_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
    {
        if (args.Status == ClientStatus.Closed)
        {
            
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

    public override void ClientSend()
    {
        if (!string.IsNullOrWhiteSpace(ClientSendText))
        {
            QueryServerTimeRequest request = new QueryServerTimeRequest()
            {
                Name = "jackson",
                Message = ClientSendText
            };
            QueryServerTimeResponse response = _tcpClient.QueryAsync<QueryServerTimeResponse>(request).Result;
        }
    }

    public override void ConnectToServer()
    {
        _tcpClient.AutoReconnect = EnableAutoReconnect;
        _tcpClient.ConnectAsync("127.0.0.1", _serverPort, (isConnected) =>
        {
                
        });
    }

    public override void Disconnect()
    {
        _tcpClient.Close();
    }
}
```
<br>  

### 4. Speed Control
The server has a very useful feature for speed limit.
The default limit speed is 10MB/s for each client. To change the default value , see the following code:
``` CSharp
private void StartServer()
{
    TcpServerConfig serverConfig = new TcpServerConfig()
    {
        //To disable speed limit, set the value to -1
        ReceiveDataMaxSpeed = 5 * 1024 * 1024,
        SendDataMaxSpeed = 5 * 1024 * 1024
    };
    _tcpServer.Start(9999, serverConfig);
}
```
<br>

## TcpProxy Usage
The library provides a TCP proxy class called TcpProxyChannel which is simple and very easy to use. 
You can use this class to monitoring communication data and even modify the data.

Sample:
``` CSharp
using SiS.Communication.Business;
using SiS.Communication.Tcp;

namespace TcpProxy.ViewModel
{
    public class ProxyChannelTest : ITcpProxyDataFilter
    {
        public ProxyChannelTest()
        {
            _proxyChannel = new TcpProxyChannel();
            _proxyChannel.DataFilter = this;
            _proxyChannel.ClientCountChanged += _proxyChannel_ClientCountChanged;
        }
        private TcpProxyChannel _proxyChannel;

        //we can get the clients count according to the ClientCountChanged event handler.
        private void _proxyChannel_ClientCountChanged(object sender, ClientCountChangedEventArgs args)
        {
            int clientsCount = args.NewCount;
        }

        public void StartService(int listenPort, string remoteIP, int remotePort)
        {
            if (!_proxyChannel.IsRunning)
            {
                _proxyChannel.ListenPort = listenPort;
                _proxyChannel.RemoteIP = remoteIP;
                _proxyChannel.RemotePort = remotePort;
                _proxyChannel.Start();
            }
        }

        public void StopService()
        {
            _proxyChannel.Stop();
        }

        //The implements for ITcpProxyDataFilter
        //We can retrieve or modify the communication data in the following function.
        public void BeforeClientToServer(TcpRawMessage clientMessage)
        {

        }
        
        public void BeforeServerToClient(TcpRawMessage serverMessage)
        {

        }
    }
}

```

<br>

## HTTP Server Usage
The http server provided in library is lightweight but efficient. The performance under windows operating system is better
than linux, because the underlying implementations of socket are different. I compared the performance of the popular http servers.
The test enviroment is that 200 threads request the same static resources at the same time, and each thread will get 20 static files, include js, css, html and so on.
So the total request count is 4000. I recorded the total requested time(in seconds) and got a form:

|Platform|IIS|Apache|Nginx|SiS|
|:-:|:-:|:-:|:-:|:-:|
| Linux | X | 5.5/900(gzip) | 3.3/300(gzip) | 200/480(gzip) |
| Windows | 3.7(gzip) | 8.4/14.8(gzip) | 152/278(gzip) |50/87(gzip) |

According to the above results, it cost more time if the server enable gzip compression.  
Because the CPU performance of the linux server used for testing is very poor, so under linux system, 
the gzip compression cost a lot of time. 


<font color=red>Note: The http server is not supported on .net Framework 4.0. </font>

Sample:

``` CSharp
    public class HttpViewModel : PageBaseViewModel
    {
        public HttpViewModel(string title) : base(title)
        {
            _httpServer = new HttpServer();
            _websocketHandler = new WebsocketHandler("/ws");
            _staticFileHandler = new StaticFileHandler();
            _staticFileHandler.DefaultFiles.Add("index.html");
            _httpServer.Handlers.Add(_websocketHandler);
            _httpServer.Handlers.Add(_staticFileHandler);
            _httpServer.UnhandledRequestReceived += _httpServer_UnhandledRequestReceived;
            _httpServer.WebSocketDataReceived += _httpServer_WSDataReceived;
            _httpServer.WebSocketStatusChanged += _httpServer_WebSocketStatusChanged;
        }
        
        private HttpServer _httpServer;
        private StaticFileHandler _staticFileHandler;
        private WebsocketHandler _websocketHandler;


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

        private void _httpServer_WSDataReceived(object sender, WebSocketDataReceivedEventArgs args)
        {
            WebSocketPacket wspacket = args.DataPacket;
            if (wspacket.DataType == WSPacketType.Text)
            {
                string strText = Encoding.UTF8.GetString(wspacket.Data.Array, wspacket.Data.Offset, wspacket.Data.Count);

            }
        }

        private void _httpServer_WebSocketStatusChanged(object sender, WebSocketStatusChangedEventArgs args)
        {

        }

        private void _httpServer_UnhandledRequestReceived(object sender, UnhandledRequestReceivedEventArgs args)
        {
            HttpRequestMessage requestMsg = args.Context.Request;
            if (requestMsg.Content is BlockStreamContent StreamContent)
            {
                BlockStream stream = StreamContent.Stream;
                FileStream fs = null;
                try
                {
                    fs = File.Open("d:\\upload\\test.zip", FileMode.Create, FileAccess.Write);
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
            else if (requestMsg.Content is ByteArrayContent)
            {

            }
        }

        public void StartServer()
        {
            if (_httpServer.IsRunning)
            {
                return;
            }
            _staticFileHandler.RootDir = "e:\\wwwroot";
            _staticFileHandler.EnableGZIP = true;
            HttpServerConfig config = new HttpServerConfig();
            config.TcpConfig.MaxPendingCount = 10000;
            config.TcpConfig.MaxClientCount = 2000;
            config.TcpConfig.SocketAsyncBufferSize = 100 * 1024;
            try
            {
                _httpServer.Start(80, config);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void StopServer()
        {
            _httpServer.Stop();
        }

        public void WebsocketSend()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Please select the client that you want to send message.");
                return;
            }

            try
            {
                _httpServer.WebSocketSendText(SelectedClient, "this is websocket client");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void WebsocketDisconnect()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Please select the client that you want to disconnect.");
                return;
            }
            try
            {
                _httpServer.WebSocketCloseClient(SelectedClient);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
```
<br>

Inherit from HttpHandler class, the users can define their own handlers to handle http requests.

Create a handler:
``` CSharp
public class MyHttpHandler : HttpHandler
{
    public override void Process(HttpContext context)
    {
        //The user must set the Response proprety of the context, which indicating the request is handled. 
        //If the Response is not set, the request will be passed to next handler. If none of handler handle the request, 
        //the UnhandledRequestReceived event of HttpServer will be triggered.
        context.Response = ResponseMsgHelper.CreateSimpleRepMsg();
    }
}
```

Add the handler to http server:
``` CSharp
_httpServer.Handlers.Add(new MyHttpHandler());
```

<br>

## UDP Usage
The UdpServer class is provided for UDP Communication. It uses a receving queue internally to avoid packet loss. 
It's also very easy to use multicast.
<br>

Server:
``` CSharp
using SiS.Communication.Udp;
using System.Net;

namespace SiS.Communication.Demo
{
    public class UdpServerViewModel
    {
        public UdpServerViewModel()
        {
            _udpServer = new UdpServer();
            _udpServer.MessageReceived += _udpServer_MessageReceived;
        }
		
        private bool _useMulticast = true;
        private UdpServer _udpServer;

        private void _udpServer_MessageReceived(object sender, UdpMessageReceivedEventArgs args)
        {
            string text = _udpServer.TextEncoding.GetString(args.Message.MessageData);
            //ServerRecvText += text + "\r\n";
        }

        public void SendGroupMessage()
        {
            if (_udpServer.IsRunning)
            {
                byte[] messageData = _udpServer.TextEncoding.GetBytes("I am server");
                _udpServer.SendGroupMessage(messageData, 9002);
            }
        }

        public void ServerSend()
        {
            if (_udpServer.IsRunning)
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9002);
                _udpServer.SendText(ipEndPoint, "I am server");
            }
        }

        public void StartServer()
        {
            if (_useMulticast)
            {
                //use multicast
                _udpServer.Start("224.1.1.1", 9001);
            }
            else
            {
                _udpServer.Start(9001);
            }
        }

        public void StopServer()
        {
            _udpServer.Stop();
        }

    }
```
<br>

Client:
``` CSharp
    public class UdpClientViewModel
    {
        public UdpClientViewModel()
        {
            _udpClient = new UdpServer();
            _udpClient.MessageReceived += _udpClient_MessageReceived;
        }

        private bool _useMulticast = true;
        private UdpServer _udpClient;

        private void _udpClient_MessageReceived(object sender, UdpMessageReceivedEventArgs args)
        {
            string text = _udpClient.TextEncoding.GetString(args.Message.MessageData);
            //ClientRecvText += text + "\r\n";
        }

        public void SendGroupMessage()
        {
            if (_useMulticast && _udpClient.IsRunning)
            {
                byte[] messageData = _udpClient.TextEncoding.GetBytes("I am client");
                _udpClient.SendGroupMessage(messageData, 9001);
            }
        }

        public void ClientSend()
        {
            if (_udpClient.IsRunning)
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9001);
                _udpClient.SendText(ipEndPoint, "I am client");
            }
        }

        public void ConnectToServer()
        {
            if (_useMulticast)
            {
                //use multicast
                _udpClient.Start("224.1.1.1", 9002);
            }
            else
            {
                _udpClient.Start(9002);
            }
        }

        public void Disconnect()
        {
            _udpClient.Stop();
        }
    }
}

```
<br>

## Process Communication
### 1.Pipe

Server:
``` CSharp
public class PipeServerViewModel
    {
        public PipeServerViewModel()
        {
            _pipeServer = new PipeServer();
            _pipeServer.ClientStatusChanged += _pipeServer_ClientStatusChanged;
            _pipeServer.MessageReceived += _pipeServer_MessageReceived; ;
        }

        private PipeServer _pipeServer;
        private void _pipeServer_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _pipeServer.TextEncoding.GetString(args.Data);
            //ServerRecvText += text + "\r\n";
        }

        private void _pipeServer_ClientStatusChanged(object sender, PipeClientStatusChangedEventArgs args)
        {

        }

        public void ServerSend()
        {
            if (_pipeServer.Status == ClientStatus.Connected)
            {
                byte[] messageData = _pipeServer.TextEncoding.GetBytes("I am server");
                _pipeServer.SendMessage(messageData);
            }
        }

        public void StartServer()
        {
             _pipeServer.Start("PipeNameXXX");
        }

        public void StopServer()
        {
            _pipeServer.Stop();
        }
    }
```

Client:
``` CSharp
public class PipeClientViewModel
    {
        public PipeClientViewModel()
        {
            _pipeClient = new PipeClient();
            _pipeClient.ClientStatusChanged += _pipeClient_ClientStatusChanged;
            _pipeClient.MessageReceived += _pipeClient_MessageReceived;
        }

        private PipeClient _pipeClient;

        private void _pipeClient_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _pipeClient.TextEncoding.GetString(args.Data);
            //ClientRecvText += text + "\r\n";
        }

        private void _pipeClient_ClientStatusChanged(object sender, PipeClientStatusChangedEventArgs args)
        {
            if (args.Status == ClientStatus.Closed)
            {

            }
        }

        public void ClientSend()
        {
            if (_pipeClient.Status == ClientStatus.Connected)
            {
                _pipeClient.SendText(ClientSendText.Trim());
            }
        }

        public void ConnectToServer()
        {
            _pipeClient.Connect("PipeNameXXX");
        }

        public void Disconnect()
        {
            _pipeClient.Close();
        }
    }
```
<br>

### 2.Share Memory Simplex
ShareMemoryReader and ShareMemoryWriter are used in share memory simplex communication. 
In this mode of communication, data can only be transmitted from one end to the other, not back.

Server:
``` CSharp
    public class ShareMemoryServerViewModel
    {
        public ShareMemoryServerViewModel()
        {
            _shareMemServer = new ShareMemoryReader("MemTest", 1024 * 1024);
            _shareMemServer.MessageReceived += _shareMemServer_MessageReceived; ;
        }

        private ShareMemoryReader _shareMemServer;

        private void _shareMemServer_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _shareMemServer.TextEncoding.GetString(args.Data);
            ServerRecvText += text + "\r\n";
        }

        public void StartServer()
        {
            _shareMemServer.Open();
        }

        public void StopServer()
        {
            _shareMemServer.Close();
        }
    }
```

Client:
``` CSharp
    public class ShareMemoryClientViewModel
    {
        public ShareMemoryClientViewModel()
        {
            _shareMemClient = new ShareMemoryWriter("MemTest", 1024 * 1024);
        }

        private ShareMemoryWriter _shareMemClient;

        public void ClientSend()
        {
            if (_shareMemClient.IsOpen)
            {
                _shareMemClient.SendText("I am client");
            }
        }

        public void ConnectToServer()
        {
            _shareMemClient.Open();
        }

        public void Disconnect()
        {
            _shareMemClient.Close();
        }
    }
```

### 3.Share Memory Duplex
The ShareMemoryDuplex class is used in share memory duplex communication. 
In this communication mode, data can be transmitted bidirectionally.Comparing with simplex communication, 
duplex communication will cost more system resources. Before using, 
please choose the appropriate communication mode according to the requirements.
<br>

Server:
``` CSharp
    public class ShareMemoryDuplexServerViewModel
    {
        public ShareMemoryDuplexServerViewModel()
        {
            _shareMemServer = new ShareMemoryDuplex(true, "MemDuplexTest", 1024 * 1024);
            _shareMemServer.MessageReceived += _shareMemServer_MessageReceived;
        }

        private ShareMemoryDuplex _shareMemServer;

        private void _shareMemServer_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _shareMemServer.TextEncoding.GetString(args.Data);
        }

        public void ServerSend()
        {
            if (_shareMemServer.IsOpen)
            {
                _shareMemServer.SendText("I am server");
            }
        }

        public  void StartServer()
        {
            _shareMemServer.Open();
            CanStartServer = false;
        }

        public void StopServer()
        {
            _shareMemServer.Close();
        }
    }
```

Client:
``` CSharp
	public class ShareMemoryDuplexClientViewModel
    {
        public ShareMemoryDuplexClientViewModel()
        {
            _shareMemClient = new ShareMemoryDuplex(false, "MemDuplexTest", 1024 * 1024);
            _shareMemClient.MessageReceived += _shareMemClient_MessageReceived;
        }

        private ShareMemoryDuplex _shareMemClient;

        private void _shareMemClient_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            string text = _shareMemClient.TextEncoding.GetString(args.Data);
        }

        public void ClientSend()
        {
            if (_shareMemClient.IsOpen)
            {
                _shareMemClient.SendText(ClientSendText.Trim());
            }
        }

        public void ConnectToServer()
        {
             _shareMemClient.Open();
        }

        public void Disconnect()
        {
            _shareMemClient.Close();
        }
    }
```

