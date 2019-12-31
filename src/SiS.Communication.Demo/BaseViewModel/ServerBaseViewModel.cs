namespace SiS.Communication.Demo
{
    public abstract class ServerBaseViewModel : NotifyBase
    {
        private string _serverSendText;
        public string ServerSendText
        {
            get { return _serverSendText; }
            set
            {
                if (value != _serverSendText)
                {
                    _serverSendText = value;
                    NotifyPropertyChanged(nameof(ServerSendText));
                }
            }
        }

        private string _serverRecvText;
        public string ServerRecvText
        {
            get { return _serverRecvText; }
            set
            {
                if (value != _serverRecvText)
                {
                    _serverRecvText = value;
                    NotifyPropertyChanged(nameof(ServerRecvText));
                }
            }
        }

        private bool _canStartServer = true;
        public bool CanStartServer
        {
            get { return _canStartServer; }
            set
            {
                if (value != _canStartServer)
                {
                    _canStartServer = value;
                    NotifyPropertyChanged(nameof(CanStartServer));
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


        public abstract void StartServer();

        public abstract void StopServer();

        public virtual void ServerSend() { }
    }
}
