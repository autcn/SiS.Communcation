using SiS.Communication;
using SiS.Communication.Business;
using System;
using System.IO;
using System.Threading;
using TcpFile.Demo.Protocol;

namespace TcpFile.Demo
{
    public class UploadFileSession
    {
        #region Constructor
        public UploadFileSession(TcpModelServer tcpServer)
        {
            UploadSessionID = Guid.NewGuid();
            _tcpServer = tcpServer;
            _cancelWaitEvent = new ManualResetEvent(false);
            _lockObject = new object();
        }
        #endregion

        #region Private Members
        private TcpModelServer _tcpServer;
        private object _lockObject;
        public Thread _cancelThread;

        private ManualResetEvent _cancelWaitEvent;
        private DateTime _lastActiveTime;
        private FileStream _fileStream;
        private bool _isRunning = false;
        #endregion

        #region Properties
        public long FileSize { get; set; }
        public string FileName { get; set; }
        public Guid UploadSessionID { get; set; }
        public string SavePath { get; set; }
        public long ClientID { get; set; }
        public Guid LastRequestID { get; set; }
        public bool IsRunning
        {
            get { return _isRunning; }
        }
        #endregion

        #region Event
        public event Action Finished;
        #endregion

        #region Private Functions

        private void CancelProc()
        {
            while (_isRunning)
            {
                //If there is no data in 8 seconds, the uploading should be cancelled.
                if (_cancelWaitEvent.WaitOne(1000) || DateTime.Now - _lastActiveTime > TimeSpan.FromSeconds(8))
                {
                    _isRunning = false;
                    lock (_lockObject)
                    {
                        _fileStream.Close();
                        try
                        {
                            File.Delete(SavePath);
                        }
                        catch { }
                    }
                    Finished?.Invoke();
                    return;
                }
            }
        }

        #endregion

        #region Public Functions

        public void ProcessBeginRequest()
        {
            UploadFileBeginResponse response = new UploadFileBeginResponse()
            {
                AllowUpload = true,
                RequestID = LastRequestID,
                UploadSessionID = UploadSessionID
            };
            if (IsRunning)
            {
                response.AllowUpload = false;
                response.Message = "can not upload a new file because the last uploading is running.";
                _tcpServer.SendModelMessage(ClientID, response);
                return;
            }

            try
            {
                _fileStream = File.Open(SavePath, FileMode.Create, FileAccess.Write);
            }
            catch (Exception ex)
            {
                response.AllowUpload = false;
                response.Message = ex.Message;
                _tcpServer.SendModelMessage(ClientID, response);
                return;
            }

            _tcpServer.SendModelMessage(ClientID, response);
            _isRunning = true;
            _cancelWaitEvent.Reset();
            _lastActiveTime = DateTime.Now;
            _cancelThread = ThreadEx.Start(CancelProc);
        }

        public void SaveFileData(ArraySegment<byte> data)
        {
            if (!_isRunning)
            {
                return;
            }
            lock (_lockObject)
            {
                _fileStream.Write(data.Array, data.Offset, data.Count);
                _lastActiveTime = DateTime.Now;
            }
        }

        public void ProcessEndRequest(DateTime lastWriteTime)
        {
            UploadFileEndResponse endResponse = new UploadFileEndResponse()
            {
                UploadSessionID = UploadSessionID,
                RequestID = LastRequestID
            };
            if (!_isRunning)
            {
                endResponse.Success = false;
                endResponse.Message = "the uploading is not running";
            }
            else
            {
                try
                {
                    _fileStream.Close();
                    File.SetLastWriteTime(SavePath, lastWriteTime);
                    endResponse.Success = true;
                }
                catch (Exception ex)
                {
                    endResponse.Success = false;
                    endResponse.Message = ex.Message;
                }
            }

            _tcpServer.SendModelMessage(ClientID, endResponse);
            _isRunning = false;
            Finished?.Invoke();
        }

        public void Cancel()
        {
            if (!_isRunning)
            {
                return;
            }
            _cancelWaitEvent.Set();
        }

        public void SendCancelResponse()
        {
            UploadFileCancelResponse cancelResponse = new UploadFileCancelResponse()
            {
                UploadSessionID = UploadSessionID,
                RequestID = LastRequestID
            };
            if (!_isRunning)
            {
                cancelResponse.Success = false;
                cancelResponse.Message = "the uploading is not running";
            }
            else
            {
                cancelResponse.Success = true;
            }
            _tcpServer.SendModelMessage(ClientID, cancelResponse);
        }

        #endregion

    }
}
