using System.Text;

namespace SiS.Communication.Process
{
    public class ShareMemoryDuplex
    {
        #region Constructor
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

        public event DataMessageReceivedEventHandler MessageReceived;

        #endregion

        #region Properties

        public bool IsOpen
        {
            get { return _shareMemReader.IsOpen; }
        }

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

        public void Open()
        {
            if (IsOpen)
            {
                throw new AlreadyRunningException("The share memory is already opened");
            }

            _shareMemReader.Open();
            _shareMemWriter.Open();
        }

        public void SendMessage(byte[] messageData)
        {
            if (!IsOpen)
            {
                throw new NotRunningException("the share memory is not opened");
            }
            _shareMemWriter.SendMessage(messageData);
        }

        public void SendText(string text)
        {
            SendText(text, TextEncoding);
        }

        public void SendText(string text, Encoding encoding)
        {
            SendMessage(encoding.GetBytes(text));
        }

        public void Close()
        {
            _shareMemWriter.Close();
            _shareMemReader.Close();
        }

        #endregion
    }
}
