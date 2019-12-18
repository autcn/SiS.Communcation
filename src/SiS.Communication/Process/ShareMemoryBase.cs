using SiS.Communication.Spliter;
using System;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace SiS.Communication.Process
{
    /// <summary>
    /// Represents a base class of share memory communication
    /// </summary>
    public abstract class ShareMemoryBase
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Process.ShareMemoryBase
        /// </summary>
        /// <param name="uniqueName">An unique name for share memory communication</param>
        /// <param name="bufferSize">The buffer size of the share memory</param>
        public ShareMemoryBase(string uniqueName, int bufferSize)
        {
            _uniqueName = uniqueName;
            _bufferSize = bufferSize;
            _packetSpliter = new SimplePacketSpliter();
            _sendBuffer = new DynamicBuffer();
        }
        #endregion

        #region Private Members
        private const string ShareMemoryNameSuffix = "_SIS_SHARE_MEMORY";
        private const string NotifyEventNameSuffix = "_SIS_SHARE_MEMORY_EVENT";
        private const string MutexNameSuffix = "_SIS_SHARE_MEMORY_MUTEX";
        private string _uniqueName;
        private MemoryMappedViewStream _stream;
        private MemoryMappedFile _memMappedFile;
        protected EventWaitHandle _notifyEvent;
        protected Mutex _mutex;
        private int _bufferSize;
        protected IPacketSpliter _packetSpliter;
        private DynamicBuffer _sendBuffer;
        #endregion

        #region Properties

        protected bool _isOpen = false;

        /// <summary>
        /// Gets the value indicating the status of the share memory.
        /// </summary>
        /// <returns>true if the share memory is open; otherwise, false. The default is false.</returns>
        public bool IsOpen
        {
            get { return _isOpen; }
        }

        /// <summary>
        /// Gets or sets the encoding when transmitting text in share memory communication.
        /// </summary>
        /// <returns>The encoding when transmitting text in share memory communication. The default is UTF8.</returns>
        public Encoding TextEncoding { get; set; } = Encoding.UTF8;

        #endregion

        #region Private Functions


        private bool WriteDataRaw(long position, byte[] data, int offset, int count)
        {
            _stream.Position = position;
            if (count + _stream.Position > _bufferSize)
            {
                return false;
            }
            _stream.Write(data, offset, count);
            return true;
        }
        private bool WriteDataRaw(byte[] data, int offset, int count)
        {
            return WriteDataRaw(_stream.Position, data, offset, count);
        }

        private bool WriteDataRaw(long position, byte[] data)
        {
            return WriteDataRaw(position, data, 0, data.Length);
        }

        private bool WriteDataRaw(byte[] data)
        {
            return WriteDataRaw(_stream.Position, data, 0, data.Length);
        }

        private byte[] ReadDataRaw(int count)
        {
            return ReadDataRaw(_stream.Position, count);
        }

        private byte[] ReadDataRaw(long position, int count)
        {
            _stream.Position = position;
            if (_bufferSize - _stream.Position < count)
            {
                return null;
            }
            byte[] data = new byte[count];
            _stream.Read(data, 0, count);
            return data;
        }

        private void ClearData()
        {
            byte[] zeroLen = BitConverter.GetBytes((int)0);
            WriteDataRaw(0, zeroLen);
        }

        private int GetDataLength()
        {
            byte[] lenData = ReadDataRaw(0, 4);
            return BitConverter.ToInt32(lenData, 0);
        }

        private void WriteDataLength(int length)
        {
            byte[] zeroLen = BitConverter.GetBytes(length);
            WriteDataRaw(0, zeroLen);
        }

        #endregion

        #region Protected Functions

        protected void Open()
        {
            if (_isOpen)
            {
                throw new AlreadyRunningException("The share memory is already opened");
            }

            _memMappedFile = MemoryMappedFile.CreateOrOpen(_uniqueName + ShareMemoryNameSuffix, _bufferSize, MemoryMappedFileAccess.ReadWrite);
            _stream = _memMappedFile.CreateViewStream();
            bool bNew = false;
            _notifyEvent = new EventWaitHandle(false, EventResetMode.ManualReset, _uniqueName + NotifyEventNameSuffix, out bNew);
            _notifyEvent.Reset();
            _mutex = new Mutex(false, _uniqueName + MutexNameSuffix, out bNew);
            ClearData();
        }

        protected bool WriteData(byte[] data, int offset, int count)
        {
            //byte[] messageData = null;
            //if (offset == 0 && data.Length == count)
            //{
            //    messageData = data;
            //}
            //else
            //{
            //    messageData = new byte[count];
            //    Array.Copy(data, offset, messageData, 0, count);
            //}
            ArraySegment<byte> packetData = _packetSpliter.MakePacket(data, offset, count, _sendBuffer);
            int curDataLength = GetDataLength();
            int curPosition = curDataLength + 4;
            if (WriteDataRaw(curPosition, packetData.Array, offset, packetData.Count))
            {
                curDataLength += packetData.Count;
                WriteDataLength(curDataLength);
                return true;
            }
            return false;
        }

        protected bool WriteData(byte[] messageData)
        {
            return WriteData(messageData, 0, messageData.Length);
        }

        protected byte[] ReadData()
        {
            int curDataLength = GetDataLength();
            if (curDataLength == 0)
            {
                return null;
            }
            byte[] data = ReadDataRaw(4, curDataLength);
            if (data != null)
            {
                ClearData();
            }
            return data;
        }

        protected void Close()
        {
            if (_notifyEvent != null)
            {
                _notifyEvent.Close();
                _notifyEvent.Dispose();
                _notifyEvent = null;
            }

            if (_mutex != null)
            {
                _mutex.Close();
                _mutex.Dispose();
                _mutex = null;
            }
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
            if (_memMappedFile != null)
            {
                _memMappedFile.Dispose();
                _memMappedFile = null;
            }
        }

        #endregion
    }
}
