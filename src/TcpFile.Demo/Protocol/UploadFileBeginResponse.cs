using SiS.Communication.Business;
using System;

namespace TcpFile.Demo.Protocol
{
    public class UploadFileBeginResponse : ResponseMessageBase, IUploadFileMessage
    {
        public bool AllowUpload { get; set; }
        public string Message { get; set; }
        public Guid UploadSessionID { get; set; }
    }
}
