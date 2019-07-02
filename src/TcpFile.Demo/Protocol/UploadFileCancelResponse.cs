using SiS.Communication.Business;
using System;

namespace TcpFile.Demo.Protocol
{
    public class UploadFileCancelResponse : ResponseMessageBase, IUploadFileMessage
    {
        public UploadFileCancelResponse()
        {
            RequestID = Guid.NewGuid();
        }
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid UploadSessionID { get; set; }
    }
}
