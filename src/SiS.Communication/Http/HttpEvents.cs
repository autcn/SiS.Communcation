using System;
using System.Net;
using System.Net.Sockets;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Provides data for unhandled http request received event.
    /// </summary>
    public class UnhandledRequestReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The context of the http communication.
        /// </summary>
        public HttpContext Context { get; set; }

        /// <summary>
        /// The client id that associated with the connection.
        /// </summary>
        public long ClientID { get; set; }
    }

    /// <summary>
    /// Provides data for web socket data received  event.
    /// </summary>
    public class WebSocketDataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The received data packet in type of WebSocketPacket.
        /// </summary>
        public WebSocketPacket DataPacket { get; set; }

        /// <summary>
        /// The client id that associated with the connection.
        /// </summary>
        public long ClientID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }
    }

    /// <summary>
    /// Provides data for web socket client status changed event.
    /// </summary>
    public class WebSocketStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the context of the web socket client.
        /// </summary>
        public IClientContext Client { get; set; }
    }

    /// <summary>
    /// The interface that represents context of the web socket client.
    /// </summary>
    public interface IClientContext
    {
        /// <summary>
        /// Gets the client id that associated with the connection.
        /// </summary>
        long ClientID { get; }

        /// <summary>
        /// Gets whether the client is connected.
        /// </summary>
        bool IsConnected { get; }
    }

    internal class WebSocketClientContext : IClientContext
    {
        public long ClientID { get; set; }
        public SocketAsyncEventArgs ClientArgs { get; set; }
        public bool IsConnected
        {
            get { return ClientArgs != null && ClientArgs.UserToken != null && ClientArgs.AcceptSocket != null && ClientArgs.AcceptSocket.Connected; }
        }
    }

    /// <summary>
    /// Represents the method that will handle the unhandled http request received event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">A SiS.Communication.Http.UnhandledRequestReceivedEventArgs object that contains the http context.</param>
    public delegate void UnhandledRequestReceivedEventHandler(object sender, UnhandledRequestReceivedEventArgs args);

    /// <summary>
    /// Represents the method that will handle the web socket data received event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">A SiS.Communication.Http.WebSocketDataReceivedEventArgs object that contains the data packet.</param>
    public delegate void WebSocketDataReceivedEventHandler(object sender, WebSocketDataReceivedEventArgs args);

    /// <summary>
    /// Represents the method that will handle the web socket client status changed event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">A SiS.Communication.Http.WebSocketStatusChangedEventArgs object that contains the new status.</param>
    public delegate void WebSocketStatusChangedEventHandler(object sender, WebSocketStatusChangedEventArgs args);
}
