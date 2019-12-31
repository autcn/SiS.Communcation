using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiS.Communication
{
    /// <summary>
    /// Provides a buffer stream which can change size dynamically.
    /// </summary>
    public class DynamicBufferStream : Stream
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of DynamicBuffer
        /// </summary>
        /// <param name="capacity">The init size of the buffer.</param>
        public DynamicBufferStream(int capacity)
        {
            _capacity = capacity;
        }

        /// <summary>
        /// Initializes a new instance of DynamicBuffer. The init size is 4K, in bytes.
        /// </summary>
        public DynamicBufferStream() : this(4 * 1024)
        {

        }

        #endregion

        #region Private Members

        private int _capacity;
        private long _statisticCount = 0;
        private long _statisticTotalSize = 0;
        private long _statisticMaxSize = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value that represents the buffer size check period. At the end of each period, 
        /// the buffer may be adjusted according to the usage in one period.
        /// </summary>
        public UInt32 BufferCheckPeriod { get; set; } = 500;

        private byte[] _buffer;
        /// <summary>
        /// Gets the inner buffer for the data storage. 
        /// Notice that the length of the Buffer is not the data length of DynamicBufferStream, use "Length" property instead.
        /// </summary>
        public byte[] Buffer
        {
            get { return _buffer; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => true;

        private long _length = 0;
        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length { get { return _length; } }

        private long _position = 0;
        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get { return _position; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("The position can not less than 0");
                }
                else if (value > _length)
                {
                    throw new ArgumentOutOfRangeException("The position can not greater than data length.");
                }
                else
                {
                    _position = value;
                }
            }
        }
        #endregion

        #region Private functions

        private void AllocBuffer(long requiredLen)
        {
            for (int i = 0; ; i++)
            {
                long rate = (long)Math.Pow(2, i);
                long newLength = _capacity * rate;
                if (newLength >= requiredLen)
                {
                    if (_buffer != null && newLength == _buffer.Length)
                    {
                        return;
                    }
                    if (_position <= 0)
                    {
                        _buffer = new byte[newLength];
                    }
                    else
                    {
                        byte[] tempBuf = new byte[newLength];
                        System.Buffer.BlockCopy(_buffer, 0, tempBuf, 0, (int)Math.Min(_position, newLength));
                        _buffer = tempBuf;
                    }
                    return;
                }
            }
        }

        #endregion

        #region Public functions

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            if (_position >= _length)
            {
                return -1;
            }

            int val = _buffer[_position];
            _position += 1;
            return val;
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified
        ///     byte array with the values between offset and (offset + count - 1) replaced by
        ///     the bytes read from the current source
        /// </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number
        /// of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
            {
                return 0;
            }
            long remainLen = _length - _position;
            if (count > remainLen)
            {
                count = (int)remainLen;
            }

            System.Buffer.BlockCopy(_buffer, (int)_position, buffer, offset, count);
            _position += count;
            return count;
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                Position = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                Position = _position + offset;
            }
            else if (origin == SeekOrigin.End)
            {
                Position = _length - offset;
            }
            return _position;
        }

        /// <summary>
        /// Set the stream data with specific byte array. The stream length will be set to the length of the array and the position will be set to 0.
        /// </summary>
        /// <param name="data">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public void Set(byte[] data, int offset, int count)
        {
            _position = 0;
            SetLength(count);
            System.Buffer.BlockCopy(data, offset, Buffer, 0, count);
        }

        /// <summary>
        /// Sets the length of the current stream.If the postion is less than length, it will be set to length.
        /// </summary>
        /// <param name="dataLength">The desired length of the current stream in bytes.</param>
        public override void SetLength(long dataLength)
        {
            int curLength = _buffer == null ? 0 : _buffer.Length;
            if (curLength < dataLength)
            {
                AllocBuffer(dataLength);
            }
            _length = dataLength;
            if (_position > _length)
            {
                _position = Length;
            }

            _statisticCount++;
            _statisticTotalSize += _length;
            if (_length > _statisticMaxSize)
            {
                _statisticMaxSize = _length;
            }
            if (_statisticCount >= BufferCheckPeriod)
            {
                long averageSize = _statisticTotalSize / _statisticCount;
                long bestSize = Math.Max(averageSize * 4, _statisticMaxSize * 2);

                if (_buffer.Length > bestSize && _buffer.Length > _capacity * 4)
                {
                    AllocBuffer(bestSize);
                }

                _statisticCount = 0;
                _statisticMaxSize = 0;
                _statisticTotalSize = 0;
            }
        }

        /// <summary>
        /// Writes a sequence of bytes to the current
        ///     stream and advances the current position within this stream by the number of
        ///     bytes written.
        /// </summary>
        /// <param name="buffer"> An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            long requiredLength = _position + count;
            if (_buffer == null || requiredLength > _length)
            {
                SetLength(requiredLength);
            }
            System.Buffer.BlockCopy(buffer, offset, _buffer, (int)_position, count);
            _position += count;
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position
        ///     within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            long requiredLength = _position + 1;
            if (_buffer == null || requiredLength > _length)
            {
                SetLength(requiredLength);
            }
            _buffer[_position] = value;
            _position += 1;
        }

        /// <summary>
        /// Set the stream length and position to 0.
        /// </summary>
        public void Clear()
        {
            SetLength(0);
            _position = 0;
        }

        /// <summary>
        /// Closes the current stream.
        /// </summary>
        public override void Close()
        {
            base.Close();
            _length = 0;
            _position = 0;
            _buffer = null;
            _statisticCount = 0;
            _statisticTotalSize = 0;
            _statisticMaxSize = 0;
        }
        #endregion
    }
}
