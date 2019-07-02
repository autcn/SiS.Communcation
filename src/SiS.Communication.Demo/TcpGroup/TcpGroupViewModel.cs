using SiS.Communication.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SiS.Communication;
using SiS.Communication.Spliter;

namespace SiS.Communication.Demo
{
    public class TcpGroupViewModel : PageBaseViewModel
    {
        #region Constructor
        public TcpGroupViewModel() : base("Tcp Group")
        {
            ClientListVM = new List<TcpGroupClientViewModel>();
            for (int i = 0; i < 3; i++)
            {
                ClientListVM.Add(new TcpGroupClientViewModel()
                {
                    ClientSendText = $"I am Client {i + 1}"
                });
            }
        }
        #endregion

        private ServerBaseViewModel _serverVM = new TcpGroupServerViewModel();
        public override ServerBaseViewModel ServerVM
        {
            get { return _serverVM; }
        }

        public List<TcpGroupClientViewModel> ClientListVM { get; set; }
    }

    public class TcpGroupServerViewModel : TcpBasicServerViewModel
    {
        public TcpGroupServerViewModel()
        {
            _tcpServer.EnableGroup = true;
        }

        public void SendGroupMessage()
        {
            if (_tcpServer.IsRunning && !string.IsNullOrWhiteSpace(ServerSendText))
            {
                _tcpServer.SendGroupTextAsync(new List<string>() { "Manager" }, ServerSendText);
            }

        }

        public void CloseClient()
        {
            if (_serverClientID > 0)
            {
                _tcpServer.CloseClient(_serverClientID);
            }
        }
    }

    public class TcpGroupClientViewModel : TcpBasicClientViewModel
    {
        protected override void OnTcpCient_ClientStatusChanged(object sender, ClientStatusChangedEventArgs args)
        {
            base.OnTcpCient_ClientStatusChanged(sender, args);
            if (args.Status == ClientStatus.Connected)
            {
                _tcpClient.JoinGroup("Manager");
            }
        }

        public void SendGroupMessage()
        {
            if (_tcpClient.IsRunning && !string.IsNullOrWhiteSpace(ClientSendText))
            {
                _tcpClient.SendGroupTextAsync(new List<string>() { "Manager" }, ClientSendText, null, null, false);
            }
        }
    }
}
