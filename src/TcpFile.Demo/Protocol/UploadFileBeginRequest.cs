using SiS.Communication.Business;
using System;

namespace TcpFile.Demo.Protocol
{
    public class UploadFileBeginRequest : RequestMessageBase, IUploadFileMessage
    {
        public UploadFileBeginRequest()
        {
            ID = Guid.NewGuid();
        }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public Guid UploadSessionID { get; set; }
    }
}
