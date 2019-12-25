using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication
{
    /// <summary>
    /// Represents a high performance data storage ring queue.
    /// </summary>
    public class RingQueue : SimpleRingQueue
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the RingQueue
        /// </summary>
        /// <param name="capacity">The init size of the queue.</param>
        /// <param name="maxCount">The limit size of the queue.</param>
        public RingQueue(int capacity, int maxCount) : base(capacity, maxCount)
        {
            _dynamicBuffer = new DynamicBufferStream(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the RingQueue. The default init size is 8K, in bytes. The default limit size is int.MaxValue.
        /// </summary>
        public RingQueue() : this(8 * 1024, int.MaxValue)
        {

        }

        #endregion

        #region Private Members
        private DynamicBufferStream _dynamicBuffer;
        #endregion

        #region Properties

        /// <summary>
        /// Gets the buffer of the queue.Notice that the length of the Buffer is not the data length, use "DataLength" property instead.
        /// </summary>
        public byte[] Buffer
        {
            get { return _dynamicBuffer.Buffer; }
        }
        #endregion

        #region Public Functions

        /// <summary>
        /// Write buffer into the queue with offset and count.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="offset">The offset of the data.</param>
        /// <param name="count">The count of bytes for writing.</param>
        /// <returns>True if the data lenght less than limit size; otherwise false.</returns>
        public override bool Write(byte[] data, int offset, int count)
        {
            if (base.Write(data, offset, count))
            {
                _dynamicBuffer.SetLength(DataLength);
                Peek(_dynamicBuffer.Buffer, 0, DataLength);
                return true;
            }
            return false;
        }

        #endregion


    }
}
