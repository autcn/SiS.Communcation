using SiS.Communication.Business;
using System;

namespace TcpFile.Demo.Protocol
{
    public class UploadFileData: ModelMessageBase, IUploadFileMessage
    {
        public UploadFileData()
        {
        }
        public Guid UploadSessionID { get; set; }
    }
}
