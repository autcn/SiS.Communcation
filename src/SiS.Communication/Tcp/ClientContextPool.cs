using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents a pool to manage ClientContext
    /// </summary>
    public class ClientContextPool
    {
        #region Constructor
        public ClientContextPool(int maxClientsCount, int sockAsyncBufferSize)
        {
            _maxClientsCount = maxClientsCount; //Math.Min(maxClientsCount, 10000);
            _sockAsyncBufferSize = sockAsyncBufferSize;

            _bufferForAll = new byte[_sockAsyncBufferSize * _maxClientsCount];
            _socketArgs = new ConcurrentStack<SocketAsyncEventArgs>();
            for (int i = 0; i < _maxClientsCount; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(_bufferForAll, i * sockAsyncBufferSize, sockAsyncBufferSize);
                _socketArgs.Push(args);
            }
            _clients = new ConcurrentStack<ClientContext>();
            GenerateNewClient();
        }
        #endregion

        #region Private Members
        private const int _clientContextIncreaseStep = 10;
        private int _maxClientsCount;
        private int _sockAsyncBufferSize;
        private byte[] _bufferForAll;
        private ConcurrentStack<ClientContext> _clients;
        private ConcurrentStack<SocketAsyncEventArgs> _socketArgs;
        #endregion

        #region Private Functions
        private void GenerateNewClient()
        {
            for (int i = 0; i < _clientContextIncreaseStep; i++)
            {
                _clients.Push(new ClientContext());
            }
        }
        #endregion

        #region Public Functions
        public ClientContext Pop()
        {
            if (_clients.Count == 0)
            {
                GenerateNewClient();
            }
            ClientContext context;
            _clients.TryPop(out context);
            SocketAsyncEventArgs args;
            if (!_socketArgs.TryPop(out args))
            {
                throw new Exception("the count of clients exceeds the limit");
            }
            context.SockAsyncArgs = args;
            return context;
        }

        public void Push(ClientContext clientContext)
        {

            clientContext.Reset();
            _clients.Push(clientContext);
        }
        public void Clear()
        {

            _clients.Clear();
            _socketArgs.Clear();
            _bufferForAll = null;
        }
        #endregion

    }
}
