using SiS.Communication.Spliter;
using System;
using System.Text;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// Represents a base class of tcp communication
    /// </summary>
    public class TcpBase
    {
        #region Private Members
        protected bool _isRunning = false;
        protected ILog _logger;
        protected IPacketSpliter _packetSpliter;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether the tcp is running.
        /// </summary>
        /// <returns>true if the tcp is running; otherwise, false. The default is false.</returns>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        /// <summary>
        /// Gets or sets an object that implements the ILog interface.
        /// </summary>
        public ILog Logger
        {
            get { return _logger; }
            set
            {
                _logger = value;
            }
        }

        /// <summary>
        /// Get or sets the text encoding in tcp communication.
        /// </summary>
        /// <returns>The text encoding int tcp communication. The default is UTF8.</returns>
        public Encoding TextEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets or sets the packet spliter which is used to split stream data.
        /// </summary>
        /// <returns>The packet spliter.</returns>
        public IPacketSpliter PacketSpliter
        {
            get { return _packetSpliter; }
            set
            {
                if (_isRunning)
                {
                    throw new Exception("can not change packet spliter during running time");
                }
                _packetSpliter = value;
            }
        }
        #endregion
    }
}
