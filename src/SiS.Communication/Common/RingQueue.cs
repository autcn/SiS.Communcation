using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication
{
    /// <summary>
    /// Represents a high performance data storage ring queue.
    /// </summary>
    public class RingQueue
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the RingQueue
        /// </summary>
        /// <param name="capacity">The init size of the queue.</param>
        /// <param name="maxCount">The limit size of the queue.</param>
        public RingQueue(int capacity, int maxCount)
        {
            _maxLimitedCount = maxCount;
            _capacity = capacity;
            _dynamicBuffer = new DynamicBuffer();
        }

        /// <summary>
        /// Initializes a new instance of the RingQueue. The default init size is 8K, in bytes. The default limit size is int.MaxValue.
        /// </summary>
        public RingQueue() : this(8 * 1024, int.MaxValue)
        {

        }
        #endregion

        #region Private Members
        private DynamicBuffer _dynamicBuffer;
        private byte[] _buffer;
        private int _capacity;
        private int _maxLimitedCount;
        private int _startIndex = 0;
        private int _dataCount = 0;

        private int _statisticCount = 0;
        private int _statisticTotalSize = 0;
        private int _statisticMaxSize = 0;
        #endregion

        #region Properties

        public byte this[int index]
        {
            get
            {
                if (index >= _dataCount)
                    throw new Exception("the index if out of range");
                if (_startIndex + index < _buffer.Length)
                {
                    return _buffer[_startIndex + index];
                }
                else
                {
                    return _buffer[(_startIndex + index) - _buffer.Length];
                }
            }
        }

        /// <summary>
        /// Gets the data length of the buffer.
        /// </summary>
        public int DataLength
        {
            get { return _dataCount; }
        }

        /// <summary>
        /// Gets the buffer of the queue.Notice that the length of the Buffer is not the data length, use "DataLength" property instead.
        /// </summary>
        public byte[] Buffer
        {
            get { return _dynamicBuffer.Buffer; }
        }
        #endregion

        #region Private Functions
        private int FirstBufferLength
        {
            get
            {
                if (_buffer == null)
                {
                    return 0;
                }
                return _buffer.Length - _startIndex;
            }
        }

        private int SecondBufferLength
        {
            get
            {
                return _startIndex;
            }
        }

        private int FirstDataCount
        {
            get
            {
                if (_buffer == null)
                {
                    return 0;
                }
                int firstBufferLength = FirstBufferLength;
                if (_dataCount <= firstBufferLength)
                {
                    return _dataCount;
                }
                else
                {
                    return firstBufferLength;
                }
            }
        }

        private int FirstSpace
        {
            get { return FirstBufferLength - FirstDataCount; }
        }

        private int SecondDataCount
        {
            get
            {
                if (_buffer == null)
                {
                    return 0;
                }
                return _dataCount - FirstDataCount;
            }
        }
        private bool AllocBuffer(int requiredTotalLen)
        {
            if (requiredTotalLen > _maxLimitedCount)
            {
                return false;
            }

            for (int i = 0; ; i++)
            {
                int rate = (int)Math.Pow(2, i);
                int newLength = _capacity * rate;
                if (newLength >= requiredTotalLen)
                {
                    if (_buffer != null && newLength == _buffer.Length)
                    {
                        return true;
                    }

                    byte[] newBuffer = new byte[newLength];
                    if (_dataCount > 0)
                    {
                        Array.Copy(_buffer, _startIndex, newBuffer, 0, FirstDataCount);
                        if (SecondDataCount > 0)
                        {
                            Array.Copy(_buffer, 0, newBuffer, FirstDataCount, SecondDataCount);
                        }
                    }
                    _startIndex = 0;
                    _buffer = newBuffer;
                    return true;
                }
            }
        }

        private int Read(byte[] outBuffer, int offset, int count, bool isPeek)
        {
            if (count <= 0)
            {
                return 0;
            }
            if (count > _dataCount)
            {
                //throw new Exception("the read count exceed the data count");
                count = _dataCount;
            }
            int firstDataCount = FirstDataCount;
            int readFirstLen = 0;
            int readSecondLen = 0;
            if (count <= firstDataCount)
            {
                readFirstLen = count;
            }
            else
            {
                readFirstLen = firstDataCount;
                readSecondLen = count - readFirstLen;
            }
            if (readFirstLen > 0)
            {
                Array.Copy(_buffer, _startIndex, outBuffer, offset, readFirstLen);
            }

            if (readSecondLen > 0)
            {
                Array.Copy(_buffer, 0, outBuffer, offset + readFirstLen, readSecondLen);
            }
            if (!isPeek)
            {
                _dataCount -= count;
                _startIndex += count;
                if (_startIndex >= _buffer.Length)
                {
                    _startIndex -= _buffer.Length;
                }
            }
            return count;
        }
        #endregion

        #region Public Functions

        /// <summary>
        /// Write data into the queue.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data lenght less than limit size; otherwise false.</returns>
        public bool Write(byte[] data)
        {
            return Write(data, 0, data.Length);
        }

        /// <summary>
        /// Write buffer into the queue with offset and count.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="offset">The offset of the data.</param>
        /// <param name="count">The count of bytes for writing.</param>
        /// <returns>True if the data lenght less than limit size; otherwise false.</returns>
        public bool Write(byte[] data, int offset, int count)
        {
            int curBufferLength = _buffer == null ? 0 : _buffer.Length;
            int needTotalLength = _dataCount + count;
            //create new buffer
            if (needTotalLength > curBufferLength)
            {
                if (!AllocBuffer(needTotalLength))
                {
                    return false;
                }
            }

            int firstSpace = FirstSpace;
            if (firstSpace == 0)
            {
                Array.Copy(data, offset, _buffer, SecondDataCount, count);
            }
            else
            {
                int firstWriteLen = 0;
                int secondWriteLen = 0;
                if (firstSpace >= count)
                {
                    firstWriteLen = count;
                }
                else
                {
                    firstWriteLen = firstSpace;
                    secondWriteLen = count - firstWriteLen;
                }
                if (firstWriteLen > 0)
                {
                    Array.Copy(data, offset, _buffer, _startIndex + _dataCount, firstWriteLen);
                }
                if (secondWriteLen > 0)
                {
                    Array.Copy(data, offset + firstWriteLen, _buffer, SecondDataCount, secondWriteLen);
                }
            }
            _dataCount = needTotalLength;

            _statisticCount++;
            _statisticTotalSize += _dataCount;
            if (_dataCount > _statisticMaxSize)
            {
                _statisticMaxSize = _dataCount;
            }
            if (_statisticCount >= 500)
            {
                int averageSize = _statisticTotalSize / _statisticCount;
                int bestSize = Math.Max(averageSize * 4, _statisticMaxSize * 2);

                if (_buffer.Length > bestSize && _buffer.Length > _capacity * 4)
                {
                    AllocBuffer(bestSize);
                }

                _statisticCount = 0;
                _statisticMaxSize = 0;
                _statisticTotalSize = 0;
            }
            _dynamicBuffer.SetLength(_dataCount);
            Peek(_dynamicBuffer.Buffer, 0, _dataCount);
            return true;
        }

        /// <summary>
        /// Read the data into out buffer from the queue without any byte removed.
        /// </summary>
        /// <param name="buffer">The buffer to save the peek data.</param>
        /// <param name="offset">The offset of the buffer to fill.</param>
        /// <param name="count">The count of bytes to peek.</param>
        /// <returns>The real count of bytes that read into the buffer.</returns>
        public int Peek(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count, true);
        }

        /// <summary>
        /// Read the data into out buffer and remove the data from the queue.
        /// </summary>
        /// <param name="buffer">The buffer to save the peek data.</param>
        /// <param name="offset">The offset of the buffer to fill.</param>
        /// <param name="count">The count of bytes to peek.</param>
        /// <returns>The real count of bytes that read into the buffer.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count, false);
        }

        /// <summary>
        /// Remove count of bytes from the queue.
        /// </summary>
        /// <param name="count">The count of bytes to remove.</param>
        /// <returns>The real count of bytes removed from the queue. 
        /// If the count is bigger than the queue data length, the return value is the queue data length.</returns>
        public int Remove(int count)
        {
            if (count > _dataCount)
            {
                count = _dataCount;
            }
            _dataCount -= count;
            _startIndex += count;
            if (_startIndex >= _buffer.Length)
            {
                _startIndex -= _buffer.Length;
            }
            return count;
        }

        /// <summary>
        /// Copy all the queue data into a new byte array.
        /// </summary>
        /// <returns>The new byte array.</returns>
        public byte[] ToArray()
        {
            byte[] data = new byte[_dataCount];
            Peek(data, 0, data.Length);
            return data;
        }

        /// <summary>
        /// Clear the queue data.
        /// </summary>
        public void Clear()
        {
            _dataCount = 0;
            _startIndex = 0;
        }
        #endregion


    }
}
