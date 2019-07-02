using SiS.Communication.Business;
using System;

namespace TcpFile.Demo.Protocol
{
    public class UploadFileCancelRequest : RequestMessageBase, IUploadFileMessage
    {
        public UploadFileCancelRequest()
        {
            ID = Guid.NewGuid();
        }

        public Guid UploadSessionID { get; set; }
    }
}
