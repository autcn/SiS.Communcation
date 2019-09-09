using Microsoft.Win32;
using SiS.Communication;
using SiS.Communication.Business;
using SiS.Communication.Tcp;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using TcpFile.Demo.Protocol;

namespace TcpFile.Demo
{
    public partial class ClientWnd : Window
    {
        #region Constructor
        public ClientWnd()
        {
            InitializeComponent();

            _tcpClient = new TcpModelClient(true, (int)MessageHeader.Model, JsonModelMessageConvert.Default);
            _tcpClient.MessageReceived += _tcpClient_MessageReceived;
            _tcpClient.ClientStatusChanged += _tcpClient_ClientStatusChanged;
        }
        #endregion

        #region Private Members
        private TcpModelClient _tcpClient;
        private Task _uploadTask;
        private CancellationTokenSource _cancelTokenSource;
        private const string ServerIPAddress = "127.0.0.1";
        private const int ServerPort = 9999;
        #endregion

        #region Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbxLog.Document.Blocks.Clear();
        }

        private void _tcpClient_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
        {
            if (args.Status == SiS.Communication.ClientStatus.Closed)
            {
                btnConnect.IsEnabled = true;
                btnConnect.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDDDDD"));
            }
            else if (args.Status == SiS.Communication.ClientStatus.Connecting)
            {
                btnConnect.IsEnabled = false;
                btnConnect.Background = new SolidColorBrush(Colors.Orange);
            }
            else if (args.Status == SiS.Communication.ClientStatus.Connected)
            {
                btnConnect.IsEnabled = false;
                btnConnect.Background = new SolidColorBrush(Colors.Green);
                //join "chat" group after connected
                _tcpClient.JoinGroup("chat");
            }
        }

        private void _tcpClient_MessageReceived(object sender, TcpRawMessageReceivedEventArgs args)
        {
            GeneralMessage serverMessage = GeneralMessage.Deserialize(args.Message.MessageRawData);

            //if the server message is model
            if (serverMessage.Header == _tcpClient.HeaderIndicator)
            {
                //get model from general message
                object model = _tcpClient.ConvertToModel(serverMessage);
                if (model is ChatMessage)
                {
                    ChatMessage chatMessage = model as ChatMessage;

                    //1.Process the message in synchronized mode. The operation will block until the operation is completed.
                    AddChatMessage(chatMessage);

                    /*2.Process the message in asynchronized mode with client's associated scheduler.
                    This operation will not block. The message will be put into the queue and be processed one by one in sequence.
                    
                    Task.Factory.StartNew(() =>
                    {
                        AddChatMessage(chatMessage);
                    }, CancellationToken.None, TaskCreationOptions.None, args.Scheduler);
                    */

                    /*3.Process the message in asynchronized mode with default scheduler.
                    This operation can process many messages at the same time. 
                    The order of message processing may be different from that of receiving.
                    
                    Task.Factory.StartNew(() =>
                    {
                        AddChatMessage(chatMessage);
                    });
                    */
                }
            }
            else
            {

            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        #endregion

        #region Private Functions

        private void AddLog(string log)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                Paragraph paragraph = new Paragraph();
                Run run = new Run()
                {
                    Text = log,
                    Foreground = Brushes.SlateGray
                };
                paragraph.LineHeight = 5;
                paragraph.Inlines.Add(run);
                tbxLog.Document.Blocks.Add(paragraph);
                tbxLog.ScrollToEnd();
            }));
        }

        private void AddChatMessage(ChatMessage chatMessage)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                Paragraph paragraph1 = new Paragraph();
                paragraph1.LineHeight = 6;
                Run run1 = new Run()
                {
                    Text = chatMessage.Name + "  " + DateTime.Now.ToString("HH:mm:ss"),
                    Foreground = Brushes.Blue
                };
                paragraph1.Inlines.Add(run1);

                Paragraph paragraph2 = new Paragraph();
                paragraph2.LineHeight = 6;
                Run run2 = new Run()
                {
                    Text = chatMessage.Content,
                    Foreground = new SolidColorBrush(Color.FromArgb(chatMessage.Color[0],
                    chatMessage.Color[1], chatMessage.Color[2], chatMessage.Color[3])),
                    FontSize = chatMessage.FontSize,
                    FontFamily = new FontFamily(chatMessage.FontFamily)
                };

                paragraph2.Inlines.Add(run2);
                tbxLog.Document.Blocks.Add(paragraph1);
                tbxLog.Document.Blocks.Add(paragraph2);
                tbxLog.ScrollToEnd();
            }));
        }

        private void SetProgress(double percent, double speed)
        {
            if (percent < 0)
            {
                percent = 0;
            }
            else if (percent > 100)
            {
                percent = 100;
            }
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                progressBar.Value = percent;
                if (speed >= 0)
                    lblRate.Content = speed.ToString("F01") + "MB/s";
            }));
        }

        private void UploadProc(object obj)
        {
            TcpModelClient uploadClient = new TcpModelClient(false, (int)MessageHeader.Model, JsonModelMessageConvert.Default);
            try
            {
                uploadClient.Connect(ServerIPAddress, ServerPort);
                SetProgress(0, 0);
                string filePath = (string)obj;
                //1.upload file request
                AddLog("Request to upload file...");
                FileInfo fi = new FileInfo(filePath);
                UploadFileBeginRequest uploadRequestMsg = new UploadFileBeginRequest()
                {
                    FileName = Path.GetFileName(filePath),
                    FileSize = fi.Length
                };
                UploadFileBeginResponse uploadReponseMsg = uploadClient.QueryAsync<UploadFileBeginResponse>(uploadRequestMsg).Result;

                if (!uploadReponseMsg.AllowUpload)
                {
                    throw new Exception("upload failed:" + uploadReponseMsg.Message);
                }
                AddLog("can upload file");
                //2.upload file data
                UploadFileData uploadData = new UploadFileData()
                {
                    UploadSessionID = uploadReponseMsg.UploadSessionID
                };
                FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] buffer = new byte[10 * 1024];
                long totalSend = 0;
                int lastTick = Environment.TickCount;
                int startTick = Environment.TickCount;
                while (true)
                {
                    //process cancellation
                    if (_cancelTokenSource.IsCancellationRequested)
                    {
                        fs.Close();
                        UploadFileCancelRequest cancelRequest = new UploadFileCancelRequest()
                        {
                            UploadSessionID = uploadReponseMsg.UploadSessionID
                        };
                        UploadFileCancelResponse cancelResponse = uploadClient.QueryAsync<UploadFileCancelResponse>(cancelRequest, int.MaxValue).Result;
                        AddLog("the uploading is cancelled");
                        return;
                    }
                    int readLen = fs.Read(buffer, 0, buffer.Length);
                    if (readLen <= 0)
                    {
                        double percent = (double)totalSend * 100.0 / (double)fi.Length;
                        double speed = (double)totalSend * 1000.0 / ((double)(Environment.TickCount - startTick) * 1024.0 * 1024.0);
                        SetProgress(percent, speed);
                        fs.Close();
                        break;
                    }
                    totalSend += readLen;
                    int curTick = Environment.TickCount;
                    if (curTick - lastTick >= 300)
                    {
                        double percent = (double)totalSend * 100.0 / (double)fi.Length;
                        double speed = (double)totalSend * 1000.0 / ((double)(curTick - startTick) * 1024.0 * 1024.0);
                        SetProgress(percent, speed);
                        lastTick = Environment.TickCount;
                    }
                    GeneralMessage generalMessage = _tcpClient.ConvertToGeneralMessage(uploadData);
                    generalMessage.Payload = new ArraySegment<byte>(buffer, 0, readLen);
                    uploadClient.SendMessage(generalMessage.Serialize());
                }

                //3.finish upload
                UploadFileEndRequest endRequest = new UploadFileEndRequest
                {
                    ID = Guid.NewGuid(),
                    LastWriteTime = fi.LastWriteTime,
                    UploadSessionID = uploadReponseMsg.UploadSessionID
                };
                UploadFileEndResponse endResponse = uploadClient.QueryAsync<UploadFileEndResponse>(endRequest, int.MaxValue).Result;
                if (endResponse.Success)
                {
                    SetProgress(100, -1);
                    AddLog("upload finished");
                }
                else
                {
                    AddLog("upload failed:" + endResponse.Message);
                }
            }
            finally
            {
                uploadClient.Close();
            }

        }

        private byte[] GetInputBoxARGB()
        {
            byte[] color = new byte[4];
            SolidColorBrush brush = tbxInputMessage.Foreground as SolidColorBrush;
            color[0] = brush.Color.A;
            color[1] = brush.Color.R;
            color[2] = brush.Color.G;
            color[3] = brush.Color.B;
            return color;
        }

        #endregion

        #region User Operations
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _tcpClient.ConnectAsync(ServerIPAddress, ServerPort, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _tcpClient.Close();
        }

        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpClient.Status != ClientStatus.Connected)
            {
                MessageBox.Show("the client is not connected to server");
                return;
            }
            btnUploadFile.IsEnabled = false;
            OpenFileDialog fileDlg = new OpenFileDialog();
            if (fileDlg.ShowDialog().Value)
            {
                try
                {
                    _cancelTokenSource = new CancellationTokenSource();
                    _uploadTask = Task.Factory.StartNew((UploadProc), fileDlg.FileName, _cancelTokenSource.Token);
                    await _uploadTask;
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        AddLog(ex.InnerException.Message);
                    }
                    else
                    {
                        AddLog(ex.Message);
                    }
                }
                btnUploadFile.IsEnabled = true;
            }
            else
            {
                btnUploadFile.IsEnabled = true;
            }
        }

        private async void BtnUploadFileCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_uploadTask == null || _uploadTask.Status != TaskStatus.Running)
            {
                return;
            }
            btnCancelUpload.IsEnabled = false;
            _cancelTokenSource.Cancel();
            await _uploadTask;
            btnCancelUpload.IsEnabled = true;

        }

        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = tbxInputMessage.Text.Trim();
            if (message == "")
            {
                return;
            }
            string name = tbxName.Text.Trim();
            if (name == "")
            {
                MessageBox.Show("the name is required.");
                return;
            }
            ChatMessage chatMessage = new ChatMessage()
            {
                Name = name,
                Content = message,
                FontSize = tbxInputMessage.FontSize,
                FontFamily = tbxInputMessage.FontFamily.ToString(),
                Color = GetInputBoxARGB()
            };
            
            try
            {
                _tcpClient.SendGroupModelMessage(_tcpClient.GroupArray, chatMessage, true);
                tbxInputMessage.Clear();
                tbxInputMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnFont_Click(object sender, RoutedEventArgs e)
        {
            byte[] curColor = GetInputBoxARGB();
            string fontFamily = tbxInputMessage.FontFamily.ToString();
            System.Drawing.Font curFont = new System.Drawing.Font(fontFamily, (float)tbxInputMessage.FontSize);

            System.Windows.Forms.FontDialog fontDlg = new System.Windows.Forms.FontDialog();
            fontDlg.ShowColor = true;
            fontDlg.Color = System.Drawing.Color.FromArgb(curColor[0], curColor[1], curColor[2], curColor[3]);
            fontDlg.Font = curFont;

            if (fontDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbxInputMessage.FontFamily = new FontFamily(fontDlg.Font.FontFamily.Name);
                tbxInputMessage.FontSize = fontDlg.Font.Size;
                tbxInputMessage.Foreground = new SolidColorBrush(Color.FromArgb(fontDlg.Color.A,
                    fontDlg.Color.R, fontDlg.Color.G, fontDlg.Color.B));
            }
        }

        #endregion
    }
}
