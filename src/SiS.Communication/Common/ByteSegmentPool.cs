using System;
using System.Collections.Generic;

namespace SiS.Communication
{
    /// <summary>
    /// Represents a byte segment pool which avoid frequent memory allocation and release.
    /// </summary>
    internal class ByteSegmentPool
    {
        #region Constructor
        /// <summary>
        /// Create an instance of ByteSegmentPool with specific client count and buffer size.
        /// </summary>
        /// <param name="maxClientCount">Estimated maximum number of clients.</param>
        /// <param name="oneBufferSize">The buffer size for each client.</param>
        public ByteSegmentPool(int maxClientCount, int oneBufferSize)
        {
            _maxClientCount = maxClientCount;
            _oneBufferSize = oneBufferSize;
            _bufferPool = new Stack<ArraySegment<byte>>();
        }
        #endregion

        #region Privte members
        private int _oneBufferSize;
        private int _maxClientCount;
        private Stack<ArraySegment<byte>> _bufferPool;
        #endregion

        #region Public functions

        /// <summary>
        /// Get buffer from the pool.
        /// </summary>
        /// <returns>An byte array segment.</returns>
        public ArraySegment<byte> GetBuffer()
        {
            lock (_bufferPool)
            {
                if (_bufferPool.Count == 0)
                {
                    int maxSize = 1024 * 1024 * 200;
                    int allocSize = _maxClientCount * _oneBufferSize / 5;
                    allocSize = Math.Min(allocSize, maxSize);
                    byte[] currentBuffer = new byte[allocSize];
                    int index = 0;
                    do
                    {
                        ArraySegment<byte> newBuffer = new ArraySegment<byte>(currentBuffer, index, _oneBufferSize);
                        index += _oneBufferSize;
                        _bufferPool.Push(newBuffer);
                    } while (index + _oneBufferSize <= allocSize);
                }
                return _bufferPool.Pop();
            }
        }

        /// <summary>
        /// Recycle the buffer to the pool.
        /// </summary>
        /// <param name="buffer"></param>
        public void RecycleBuffer(ArraySegment<byte> buffer)
        {
            lock (_bufferPool)
            {
                _bufferPool.Push(buffer);
            }
        }
        #endregion
    }
}
