using System;
using System.Collections.Generic;
using System.IO.Pipes;

namespace SiS.Communication.Process
{
    /// <summary>
    /// Represents a pipe client object that derived from SiS.Communication.Process.PipeBase
    /// </summary>
    public class PipeClient : PipeBase
    {
        #region Public Functions
        /// <summary>
        /// Connect to a named pipe
        /// </summary>
        /// <param name="pipeName">The pipe name to connect</param>
        public void Connect(string pipeName)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException("The client is already running");
            }
            _pipeName = pipeName;
            _pipeStream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            NamedPipeClientStream clientStream = _pipeStream as NamedPipeClientStream;
            _isRunning = true;
            try
            {
                clientStream.Connect(0);
                if (clientStream.IsConnected)
                {
                    NotifyStatusChanged(ClientStatus.Connected);
                }
            }
            catch (System.IO.IOException)
            {
                clientStream.Dispose();
                _isRunning = false;
                _pipeStream = null;
                throw new Exception("The server is connected to another client");
            }
            catch
            {
                NotifyStatusChanged(ClientStatus.Connecting);
            }
            _taskScheduler = new SingleThreadTaskScheduler();
            _workThread = ThreadEx.Start(ConnectProc);
        }

        /// <summary>
        /// Close the pipe connection 
        /// </summary>
        public void Close()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            _pipeStream.Dispose();
            _workThread.Join();
            _taskScheduler.Stop();
            NotifyStatusChanged(ClientStatus.Closed);
        }

        #endregion

        #region Private Functions
        private void ConnectProc()
        {
            NamedPipeClientStream pipeClient = _pipeStream as NamedPipeClientStream;
            while (_isRunning)
            {
                if (!pipeClient.IsConnected)
                {
                    try
                    {
                        pipeClient.Connect(200);
                        if (pipeClient.IsConnected)
                        {
                            _syncContext.Post((state) =>
                           {
                               NotifyStatusChanged(ClientStatus.Connected);
                           }, null);
                        }
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                    catch (System.IO.IOException)
                    {
                        _isRunning = false;
                        _pipeStream.Dispose();
                        _syncContext.Post((state) =>
                          {
                              NotifyStatusChanged(ClientStatus.Closed);
                          }, null);
                        return;
                    }
                }
                else
                {
                    break;
                }
            }
            try
            {
                byte[] buffer = new byte[2048];
                while (_isRunning)
                {
                    if (!_pipeStream.IsConnected)
                    {
                        throw new Exception();
                    }
                    int readLen = _pipeStream.Read(buffer, 0, buffer.Length);
                    if (readLen > 0)
                    {
                        _recvQueue.Write(buffer, 0, readLen);
                        int endPos = 0;
                        List<DataPacket> packets = _packetSpliter.GetPackets(_recvQueue.Buffer, 0, _recvQueue.DataLength, 0, out endPos);
                        if (packets != null && packets.Count > 0)
                        {
                            _recvQueue.Remove(endPos);
                            foreach (DataPacket message in packets)
                            {
                                NotifyMessageReceived(message);
                            }
                        }
                    }
                }
            }
            catch
            {
                if (_isRunning)
                {
                    _isRunning = false;
                    _pipeStream.Dispose();
                    _taskScheduler.Stop();
                    //restart
                    _syncContext.Post((state) =>
                    {
                        Connect(_pipeName);
                    }, null);
                }
            }
        }

        #endregion

    }
}
