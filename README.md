# SiS.Communcation
## Features
This project provides a variety of communication components, including TCP, UDP, process communication. 
TCP communication is very powerful, efficient and easy to use.
## TCP Usage
### 1.Basic Usage
Use the default spliter to split data stream into packets. 
The default transport  packet structure is :  

| 4 bytes| Length bytes |
|:-:|:-:|
| Length | Message Content |

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
There are four packet spliters built-in.  
**2.1 SimplePacketSpliter**

| 4 bytes| Length bytes |
|:-:|:-:|
| Length | Message Content |

The length is a 32-bit integer in host network order. If network byte order is used,  please use the following code:
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

### 3. Model Transmission
It is very easy to transfer models using this library.  

First, we should define a model convert. The example:

``` CSharp
using Newtonsoft.Json;
using SiS.Communication.Business;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace SiS.Communication.Demo
{
    public class JsonModelMessageConvert : ModelMessageConvert
    {
        public static JsonModelMessageConvert Default { private set; get; } = new JsonModelMessageConvert();
        private Dictionary<string, Type> _registeredTypeDict;
        protected override T Deserialize<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        protected override string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        
        protected override Dictionary<string, Type> RegisteredTypeDict
        {
            get
            {
                if (_registeredTypeDict == null)
                {
                    _registeredTypeDict = new Dictionary<string, Type>();
                    List<Type> children = GetTypeDescendants(Assembly.GetExecutingAssembly(), typeof(ModelMessageBase)).ToList();
                    foreach (Type type in children)
                    {
                        _registeredTypeDict.Add(type.FullName, type);
                    }
                }
                return _registeredTypeDict;
            }
        }
    }
}

```
Then, we define an enumeration for message header type.
``` CSharp
    public enum MessageHeader : int
    {
        Model = 1,
        Other
    }
```

The server example:
``` CSharp
public class TcpModelServerViewModel : ServerBaseViewModel
{
    public TcpModelServerViewModel()
    {
        _tcpServer = new TcpModelServer((int)MessageHeader.Model, JsonModelMessageConvert.Default);
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
        _tcpClient = new TcpModelClient(true, (int)MessageHeader.Model, JsonModelMessageConvert.Default);
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
The default limit speed is 10MB/s for each client. To change the default limit speed , see the following code:
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


