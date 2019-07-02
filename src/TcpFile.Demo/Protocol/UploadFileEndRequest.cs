using SiS.Communication.Business;
using System;

namespace TcpFile.Demo.Protocol
{
    public class UploadFileEndRequest : RequestMessageBase, IUploadFileMessage
    {
        public DateTime LastWriteTime { get; set; }
        public Guid UploadSessionID { get; set; }
    }
}
