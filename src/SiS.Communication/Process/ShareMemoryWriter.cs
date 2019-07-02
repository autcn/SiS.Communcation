using System.Text;

namespace SiS.Communication.Process
{
    /// <summary>
    /// Represents a ShareMemoryWriter object that derived from SiS.Communication.Process.ShareMemoryBase
    /// </summary>
    public class ShareMemoryWriter : ShareMemoryBase
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the SiS.Communication.Process.ShareMemoryWriter
        /// </summary>
        /// <param name="uniqueName">An unique name for share memory communication</param>
        /// <param name="bufferSize">The buffer size of the share memory</param>
        public ShareMemoryWriter(string uniqueName, int bufferSize) : base(uniqueName, bufferSize)
        {
        }
        #endregion

        #region Public Members

        /// <summary>
        /// Open the share memory.
        /// </summary>
        public new void Open()
        {
            base.Open();
            _isOpen = true;
        }

        /// <summary>
        /// Send message data to share memory reader.
        /// </summary>
        /// <param name="messageData">The message data be sent.</param>
        public void SendMessage(byte[] messageData)
        {
            if (!_isOpen)
            {
                throw new NotRunningException("the share memory is not opened");
            }
            _mutex.WaitOne();
            WriteData(messageData);
            _notifyEvent.Set();
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Send message text to share memory reader using default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="text">The text to be sent.</param>
        public void SendText(string text)
        {
            SendText(text, TextEncoding);
        }

        /// <summary>
        /// Send message text to share memory reader using specific encoding.
        /// </summary>
        /// <param name="text">The text to be sent.</param>
        /// <param name="encoding">The text encoding in share memory communciation.</param>
        public void SendText(string text, Encoding encoding)
        {
            SendMessage(encoding.GetBytes(text));
        }

        /// <summary>
        /// Close share memory.
        /// </summary>
        public new void Close()
        {
            if (!_isOpen)
            {
                return;
            }
            _isOpen = false;
            base.Close();
        }

        #endregion
    }
}
