using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SiS.Communication.Process
{
    /// <summary>
    /// Represents a ShareMemoryReader object that derived from SiS.Communication.Process.ShareMemoryBase
    /// </summary>
    public class ShareMemoryReader : ShareMemoryBase
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Process.ShareMemoryReader
        /// </summary>
        /// <param name="uniqueName">An unique name for share memory communication</param>
        /// <param name="bufferSize">The buffer size of the share memory</param>
        public ShareMemoryReader(string uniqueName, int bufferSize) : base(uniqueName, bufferSize)
        {
            _recvBuffer = new RingQueue();
        }
        #endregion

        #region Private Members
        private Thread _recvThread;
        private RingQueue _recvBuffer;
        SingleThreadTaskScheduler _taskScheduler;
        #endregion

        #region Events
        /// <summary>
        /// Represents the method that will handle the message received event of a SiS.Communication.Process.ShareMemoryReader object.
        /// </summary>
        public event DataMessageReceivedEventHandler MessageReceived;

        #endregion

        #region Private Functions

        private void RecvProc()
        {
            _isOpen = true;
            while (_isOpen)
            {
                //wait data
                _notifyEvent.WaitOne();
                if (!_isOpen)
                {
                    return;
                }
                _mutex.WaitOne();
                byte[] readData = ReadData();
                _notifyEvent.Reset();
                _mutex.ReleaseMutex();
                if (readData != null)
                {
                    _recvBuffer.Write(readData);
                    int endPos = 0;
                    List<ArraySegment<byte>> packets = _packetSpliter.GetPackets(_recvBuffer.Buffer, 0, _recvBuffer.DataLength, out endPos);
                    if (packets != null && packets.Count > 0)
                    {
                        _recvBuffer.Remove(endPos);
                        foreach (ArraySegment<byte> message in packets)
                        {
                            byte[] newData = message.ToArray();
                            Task.Factory.StartNew((obj) =>
                            {
                                MessageReceived?.Invoke(this, new DataMessageReceivedEventArgs() { Data = (byte[])obj });
                            }, newData, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
                        }
                    }
                }

            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Open the share memory
        /// </summary>
        public new void Open()
        {
            base.Open();
            _taskScheduler = new SingleThreadTaskScheduler();
            _recvThread = ThreadEx.Start(RecvProc);
        }

        /// <summary>
        /// Close the share memory
        /// </summary>
        public new void Close()
        {
            if (!_isOpen)
            {
                return;
            }
            _isOpen = false;
            _notifyEvent.Set();
            _recvThread.Join(2000);
            base.Close();
            _taskScheduler.Stop();
        }

        #endregion
    }
}
