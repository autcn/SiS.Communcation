using System;
using System.Collections.Generic;
using System.IO.Pipes;

namespace SiS.Communication.Process
{
    /// <summary>
    /// Represents a pipe server object that derived from SiS.Communication.Process.PipeBase
    /// </summary>
    public class PipeServer : PipeBase
    {
        #region Public Functions

        /// <summary>
        /// Start pipe server using a specific name.
        /// </summary>
        /// <param name="pipeName">The pipe name for starting a pipe server.</param>
        public void Start(string pipeName)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException("The server is already running");
            }
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("Pipe name is required");
            }
            _pipeName = pipeName;
            _taskScheduler = new SingleThreadTaskScheduler();
            _pipeStream = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _workThread = ThreadEx.Start(WorkProc);
        }

        /// <summary>
        /// Stop the pipe server.
        /// </summary>
        public void Stop()
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
        private void WorkProc()
        {
            _isRunning = true;
            NamedPipeServerStream serverStream = _pipeStream as NamedPipeServerStream;
            try
            {
                serverStream.WaitForConnection();
                _syncContext.Post((state) =>
                {
                    NotifyStatusChanged(ClientStatus.Connected);
                }, null);
                byte[] buffer = new byte[2048];
                while (_isRunning)
                {
                    if (!_pipeStream.IsConnected)
                    {
                        break;
                    }
                    int readLen = _pipeStream.Read(buffer, 0, buffer.Length);
                    if (readLen > 0)
                    {
                        _recvQueue.Write(buffer, 0, readLen);
                        int endPos = 0;
                        //byte[] data = _recvQueue.ToArray();
                        List<ArraySegment<byte>> packets = _packetSpliter.GetPackets(_recvQueue.Buffer, 0, _recvQueue.DataLength, out endPos);
                        if (packets != null && packets.Count > 0)
                        {
                            _recvQueue.Remove(endPos);
                            foreach (ArraySegment<byte> message in packets)
                            {
                                NotifyMessageReceived(message);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            if (_isRunning)
            {
                _pipeStream.Dispose();
                _isRunning = false;
                _taskScheduler.Stop();
                _syncContext.Post((state) =>
                {
                    NotifyStatusChanged(ClientStatus.Closed);
                    Start(_pipeName);
                }, null);
            }
        }
        #endregion
    }
}
