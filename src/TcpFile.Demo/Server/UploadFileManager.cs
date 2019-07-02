using SiS.Communication.Business;
using System;
using System.Collections.Concurrent;
using System.IO;
using TcpFile.Demo.Protocol;

namespace TcpFile.Demo
{
    public class UploadFileManager
    {
        #region Constructor
        public UploadFileManager(TcpModelServer tcpServer, string savePath)
        {
            _tcpServer = tcpServer;
            _sessionDict = new ConcurrentDictionary<Guid, UploadFileSession>();
            _fileSavePath = savePath;
        }
        #endregion

        #region Private Members
        private TcpModelServer _tcpServer;
        private string _fileSavePath;
        private ConcurrentDictionary<Guid, UploadFileSession> _sessionDict;
        #endregion

        #region Private Functions
        private void ProcessUploadCancel(long clientID, UploadFileCancelRequest cancelRequest)
        {
            if (_sessionDict.TryGetValue(cancelRequest.UploadSessionID, out UploadFileSession uploadSession))
            {
                uploadSession.LastRequestID = cancelRequest.ID;
                uploadSession.Cancel();
                uploadSession.SendCancelResponse();
            }
        }

        private void ProcessUploadBegin(long clientID, UploadFileBeginRequest uploadRequest)
        {
            UploadFileSession uploadSession = new UploadFileSession(_tcpServer)
            {
                FileName = uploadRequest.FileName,
                FileSize = uploadRequest.FileSize,
                ClientID = clientID,
                LastRequestID = uploadRequest.ID,
                SavePath = Path.Combine(_fileSavePath, uploadRequest.FileName)
            };
            uploadSession.Finished += () =>
            {
                _sessionDict.TryRemove(uploadSession.UploadSessionID, out UploadFileSession temp);
            };
            _sessionDict.TryAdd(uploadSession.UploadSessionID, uploadSession);
            uploadSession.ProcessBeginRequest();
        }

        private void ProcessUploadData(long clientID, UploadFileData data, ArraySegment<byte> payload)
        {
            if (_sessionDict.TryGetValue(data.UploadSessionID, out UploadFileSession uploadSession))
            {
                uploadSession.SaveFileData(payload);
            }
        }

        private void ProcessUploadEnd(long clientID, UploadFileEndRequest request)
        {
            if (_sessionDict.TryGetValue(request.UploadSessionID, out UploadFileSession uploadSession))
            {
                uploadSession.LastRequestID = request.ID;
                uploadSession.ProcessEndRequest(request.LastWriteTime);
            }
        }
        #endregion

        #region Public Functions
        public void ProcessFileMessage(long clientID, IUploadFileMessage model, ArraySegment<byte> payload)
        {
            if (model is UploadFileBeginRequest)
            {
                ProcessUploadBegin(clientID, model as UploadFileBeginRequest);
            }
            else if (model is UploadFileData)
            {
                ProcessUploadData(clientID, model as UploadFileData, payload);
            }
            else if (model is UploadFileEndRequest)
            {
                ProcessUploadEnd(clientID, model as UploadFileEndRequest);
            }
            else if (model is UploadFileCancelRequest)
            {
                ProcessUploadCancel(clientID, model as UploadFileCancelRequest);
            }
        }

        public void CloseClient(long clientID)
        {
            foreach (Guid sessionID in _sessionDict.Keys)
            {
                if (_sessionDict[sessionID].ClientID == clientID)
                {
                    _sessionDict[sessionID].Cancel();
                }
            }
        }
        #endregion
    }

}
