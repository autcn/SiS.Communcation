using System;
using System.IO;
using System.Threading;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Represent a fixed length stream than can block reading while the data is not finished.
    /// </summary>
    public class BlockStream : Stream
    {
        #region Constructor
        /// <summary>
        /// Create an instance of BlockStream
        /// </summary>
        /// <param name="length">The total length of the stream.</param>
        /// <param name="capacity">The initial size of the internal memory in bytes.</param>
        /// <param name="maxBlockSize">The max storage size of the stream. When the the length of the stream reach the max size, the writing will be blocked.</param>
        public BlockStream(int length, int capacity, int maxBlockSize)
        {
            _length = length;
            _maxBlockSize = maxBlockSize;
            _buffer = new SimpleRingQueue(capacity, int.MaxValue);
            _readWaitEvent = new ManualResetEvent(false);
            _writeWaitEvent = new ManualResetEvent(false);
            _finishWaitEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Create an instance of BlockStream.
        /// </summary>
        /// <param name="length">The total length of the stream.</param>
        public BlockStream(int length) : this(length, 10 * 1024, 4 * 1024 * 1024)
        {

        }
        #endregion

        #region Privte members
        private SimpleRingQueue _buffer;
        private ManualResetEvent _readWaitEvent;
        private ManualResetEvent _writeWaitEvent;
        private ManualResetEvent _finishWaitEvent;
        private int _maxBlockSize;
        private bool _isClosed = false;
        #endregion

        #region Properties
        private int _readTimeOut = 5000;
        /// <summary>
        ///    Gets or sets a value, in miliseconds, that determines how long the stream will
        ///     attempt to read before timing out.
        /// </summary>
        public override int ReadTimeout { get => _readTimeOut; set => _readTimeOut = value; }
        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        public override bool CanTimeout => true;

        /// <summary>
        /// Gets a value indicating whether the current
        ///     stream supports reading.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the current
        ///     stream supports seeking.
        /// </summary>

        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the current
        ///     stream supports writing.
        /// </summary>
        public override bool CanWrite => true;

        private long _length = 0;
        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length { get { return _length; } }

        private long _position = 0;
        /// <summary>
        /// Gets the position within the current
        ///     stream.The sets method is not supported.
        /// </summary>
        public override long Position { get { return _position; } set { throw new NotSupportedException(); } }
        #endregion

        #region Events

        #endregion

        #region  Event handlers

        #endregion

        #region Private functions

        #endregion

        #region Public functions

        /// <summary>
        /// Waiting until the operations finished.
        /// </summary>
        public override void Flush()
        {
            if (!_finishWaitEvent.WaitOne(10 * 1000))
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the current
        ///     stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified
        ///     byte array with the values between offset and (offset + count - 1) replaced by
        ///     the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read
        ///     from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number
        ///     of bytes requested if that many bytes are not currently available, or zero (0)
        ///     if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position == Length)
            {
                return 0;
            }
            while (!_isClosed)
            {
                Monitor.Enter(_buffer);
                if (_buffer.DataLength > 0)
                {
                    int len = _buffer.Read(buffer, offset, count);
                    _position += len;
                    _writeWaitEvent.Set();
                    Monitor.Exit(_buffer);
                    if (_position == Length)
                    {
                        _finishWaitEvent.Set();
                    }
                    return len;
                }
                else
                {
                    _readWaitEvent.Reset();
                    Monitor.Exit(_buffer);
                    if (!_readWaitEvent.WaitOne(ReadTimeout))
                    {
                        throw new TimeoutException();
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes a sequence of bytes to the current
        ///     stream and advances the current position within this stream by the number of
        ///     bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current
        ///     stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current
        ///     stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            while (!_isClosed)
            {
                Monitor.Enter(_buffer);
                if (_buffer.DataLength + count <= _maxBlockSize)
                {
                    _buffer.Write(buffer, offset, count);
                    _readWaitEvent.Set();
                    Monitor.Exit(_buffer);
                    return;
                }
                else
                {
                    _writeWaitEvent.Reset();
                    Monitor.Exit(_buffer);
                    if (!_writeWaitEvent.WaitOne(10 * 1000))
                    {
                        throw new TimeoutException();
                    }
                }
            }
        }

        /// <summary>
        /// Closes the current stream and releases any resources associated with the current stream.
        /// </summary>
        public override void Close()
        {
            if (_isClosed)
            {
                return;
            }
            base.Close();
            _isClosed = true;
            _writeWaitEvent.Set();
            _readWaitEvent.Set();
        }
        #endregion
    }
}
