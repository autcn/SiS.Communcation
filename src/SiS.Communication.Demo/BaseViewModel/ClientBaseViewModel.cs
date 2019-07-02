using SiS.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Demo
{
    public abstract class ClientBaseViewModel : NotifyBase
    {
        private string _clientSendText;
        public string ClientSendText
        {
            get { return _clientSendText; }
            set
            {
                if (value != _clientSendText)
                {
                    _clientSendText = value;
                    NotifyPropertyChanged(nameof(ClientSendText));
                }
            }
        }

        private string _clientRecvText;
        public string ClientRecvText
        {
            get { return _clientRecvText; }
            set
            {
                if (value != _clientRecvText)
                {
                    _clientRecvText = value;
                    NotifyPropertyChanged(nameof(ClientRecvText));
                }
            }
        }

        private bool _canConnect = true;
        public bool CanConnect
        {
            get { return _canConnect; }
            set
            {
                if (value != _canConnect)
                {
                    _canConnect = value;
                    NotifyPropertyChanged(nameof(CanConnect));
                }
            }
        }

        private ClientStatus _clientStatus = ClientStatus.Closed;
        public ClientStatus ClientStatus
        {
            get { return _clientStatus; }
            set
            {
                if (value != _clientStatus)
                {
                    _clientStatus = value;
                    NotifyPropertyChanged(nameof(ClientStatus));
                }
            }
        }

        private bool _autoReconnect = false;
        public bool EnableAutoReconnect
        {
            get { return _autoReconnect; }
            set
            {
                if (value != _autoReconnect)
                {
                    _autoReconnect = value;
                    NotifyPropertyChanged(nameof(EnableAutoReconnect));
                }
            }
        }

        private bool _showAutoReconnect = true;
        public bool ShowAutoReconnect
        {
            get { return _showAutoReconnect; }
            set
            {
                if (value != _showAutoReconnect)
                {
                    _showAutoReconnect = value;
                    NotifyPropertyChanged(nameof(ShowAutoReconnect));
                }
            }
        }

        private bool _canReceive = true;
        public bool CanReceive
        {
            get { return _canReceive; }
            set
            {
                if (value != _canReceive)
                {
                    _canReceive = value;
                    NotifyPropertyChanged(nameof(CanReceive));
                }
            }
        }

        private bool _canSend = true;
        public bool CanSend
        {
            get { return _canSend; }
            set
            {
                if (value != _canSend)
                {
                    _canSend = value;
                    NotifyPropertyChanged(nameof(CanSend));
                }
            }
        }



        public abstract void ConnectToServer();

        public abstract void Disconnect();

        public abstract void ClientSend();
    }
}
