using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication
{
    /// <summary>
    /// Provides a buffer which can change size dynamically.
    /// </summary>
    public class DynamicBuffer
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of DynamicBuffer
        /// </summary>
        /// <param name="capacity">The init size of the buffer.</param>
        public DynamicBuffer(int capacity)
        {
            _capacity = capacity;
        }

        /// <summary>
        /// Initializes a new instance of DynamicBuffer. The init size is 4K, in bytes.
        /// </summary>
        public DynamicBuffer() : this(4 * 1024)
        {

        }

        #endregion

        #region Private Members

        private int _capacity;
        private byte[] _buffer;
        private int _dataCount = 0;

        private int _statisticCount = 0;
        private int _statisticTotalSize = 0;
        private int _statisticMaxSize = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a the length of the buffer.
        /// </summary>
        public int DataLength
        {
            get { return _dataCount; }
        }

        /// <summary>
        /// Gets the inner buffer for the data storage. 
        /// Notice that the length of the Buffer is not the data length of DynamicBuffer, use "DataLength" property instead.
        /// </summary>
        public byte[] Buffer
        {
            get { return _buffer; }
        }

        #endregion

        #region Private Functions
        private void AllocBuffer(int requiredLen)
        {

            for (int i = 0; ; i++)
            {
                int rate = (int)Math.Pow(2, i);
                int newLength = _capacity * rate;
                if (newLength >= requiredLen)
                {
                    if (_buffer != null && newLength == _buffer.Length)
                    {
                        return;
                    }
                    _buffer = new byte[newLength];
                    return;
                }
            }
        }
        #endregion

        #region Public Functions

        /// <summary>
        /// Write the data into inner buffer.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="offset">The offset of the data array.</param>
        /// <param name="count">The count of bytes to write.</param>
        public void Write(byte[] data, int offset, int count)
        {
            SetLength(count);
            Array.Copy(data, offset, Buffer, 0, count);
        }

        /// <summary>
        /// Set the data length of DynamicBuffer.
        /// </summary>
        /// <param name="dataLength"></param>
        public void SetLength(int dataLength)
        {
            int curLength = _buffer == null ? 0 : _buffer.Length;
            if (curLength < dataLength)
            {
                AllocBuffer(dataLength);
            }
            _dataCount = dataLength;


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

        }
        #endregion
    }
}
