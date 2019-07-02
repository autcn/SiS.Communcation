using SiS.Communication.Business;
using System;

namespace TcpFile.Demo.Protocol
{
    public class UploadFileEndResponse : ResponseMessageBase, IUploadFileMessage
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid UploadSessionID { get; set; }
    }
}
