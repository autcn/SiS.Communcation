using System.Text;

namespace SiS.Communication.Process
{
    /// <summary>
    /// Represents a ShareMemoryDuplex object that used for process communication with share memory.
    /// </summary>
    public class ShareMemoryDuplex
    {
        #region Constructor
        /// <summary>
        /// Create an instance of ShareMemoryDuplex
        /// </summary>
        /// <param name="isServer">True if used as server, otherwise false.</param>
        /// <param name="uniqueName">The unique name of share memory to create.</param>
        /// <param name="bufferSize">The buffer size of share memory.</param>
        public ShareMemoryDuplex(bool isServer, string uniqueName, int bufferSize)
        {
            _shareMemReader = new ShareMemoryReader(uniqueName + (isServer ? "_SERVER" : "_CLIENT"), bufferSize);
            _shareMemReader.MessageReceived += _shareMemReader_MessageReceived;
            _shareMemWriter = new ShareMemoryWriter(uniqueName + (isServer ? "_CLIENT" : "_SERVER"), bufferSize);
        }
        #endregion

        #region Private Members
        private ShareMemoryReader _shareMemReader;
        private ShareMemoryWriter _shareMemWriter;
        #endregion

        #region Events

        /// <summary>
        /// Represents the method that will handle the message received event of a SiS.Communication.Process.ShareMemoryDuplex object.
        /// </summary>
        public event DataMessageReceivedEventHandler MessageReceived;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value indicating the status of the share memory.
        /// </summary>
        /// <returns>true if the share memory is open; otherwise, false. The default is false.</returns>
        public bool IsOpen
        {
            get { return _shareMemReader.IsOpen; }
        }

        /// <summary>
        /// Gets or sets the text encoding when sending text message.
        /// </summary>
        public Encoding TextEncoding
        {
            get { return _shareMemWriter.TextEncoding; }
            set { _shareMemWriter.TextEncoding = value; }
        }

        #endregion

        #region Private Functions
        private void _shareMemReader_MessageReceived(object sender, DataMessageReceivedEventArgs args)
        {
            MessageReceived?.Invoke(this, args);
        }
        #endregion

        #region Public Functions

        /// <summary>
        /// Open the share memory
        /// </summary>
        public void Open()
        {
            if (IsOpen)
            {
                throw new AlreadyRunningException("The share memory is already opened");
            }

            _shareMemReader.Open();
            _shareMemWriter.Open();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageData"></param>
        public void SendMessage(byte[] messageData)
        {
            if (!IsOpen)
            {
                throw new NotRunningException("the share memory is not opened");
            }
            _shareMemWriter.SendMessage(messageData);
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
        public void Close()
        {
            _shareMemWriter.Close();
            _shareMemReader.Close();
        }

        #endregion
    }
}
