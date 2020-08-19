using SiS.Communication;
using SiS.Communication.Business;
using SiS.Communication.Tcp;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using TcpFile.Demo.Protocol;

namespace TcpFile.Demo
{
    public partial class ServerWnd : Window
    {
        #region Constructor
        public ServerWnd()
        {
            InitializeComponent();

            _logWriter = new LogWriter("c:\\TcpFile.Demo.Log");
            _logWriter.OutputLevel = LogLevel.All;

            _tcpServer = new TcpModelServer((int)MessageHeader.Model);
            _tcpServer.EnableGroup = true;
            _tcpServer.MessageReceived += _tcpServer_MessageReceived;
            _tcpServer.ClientStatusChanged += _tcpServer_ClientStatusChanged;
            _tcpServer.Logger = _logWriter;

            string savePath = "D:\\FileUpload";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            _uploadFileHandler = new UploadFileManager(_tcpServer, savePath);
        }
        #endregion

        #region Private Members
        private LogWriter _logWriter;
        private TcpModelServer _tcpServer;
        private UploadFileManager _uploadFileHandler;
        #endregion

        #region Properties

        #endregion

        #region Event Handlers
        private void _tcpServer_MessageReceived(object sender, TcpRawMessageReceivedEventArgs args)
        {
            if (!_tcpServer.IsRunning)
            {
                return;
            }
            //get GeneralMessage from raw data without detached. The message will use shared memory with the receiving buffer.
            //So this function can't be returned until the buffer used up.
            GeneralMessage clientMessage = GeneralMessage.Deserialize(args.Message.MessageRawData, false);
            //if the client message is model
            if (clientMessage.Header == _tcpServer.HeaderIndicator)
            {
                //get model from general message
                object model = _tcpServer.ConvertToModel(clientMessage);
                if (model is IUploadFileMessage)
                {
                    _uploadFileHandler.ProcessFileMessage(args.Message.ClientID, model as IUploadFileMessage, clientMessage.Payload);
                }
                else
                {
                    //detach the message from the receiving buffer, so the message can be processed at any time.
                    clientMessage.Detach();
                }
            }
            else
            {

            }
        }


        private void _tcpServer_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
        {
            string connStr = args.Status == ClientStatus.Connected ? "connected" : "disconnected";
            string logInfo = $"{args.IPEndPoint.ToString()} {connStr}\r\n";
            if (args.Status == ClientStatus.Closed)
            {
                _uploadFileHandler.CloseClient(args.ClientID);
            }
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                tbxLog.AppendText(logInfo);
                tbxLog.ScrollToEnd();
            }));

        }
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
        #endregion

        #region User Opertions
        private void BtnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _tcpServer.Start(9999);
                btnStartServer.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnStopServer_Click(object sender, RoutedEventArgs e)
        {
            _tcpServer.Stop();
            btnStartServer.IsEnabled = true;
        }
        #endregion
    }
}
