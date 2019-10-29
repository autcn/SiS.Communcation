using SiS.Communication.Business;
using SiS.Communication.Tcp;
using TcpProxy.Model;

namespace TcpProxy.ViewModel
{
    public class ProxyChannelItem : NotifyBase, ITcpProxyDataFilter
    {
        #region Constructor
        public ProxyChannelItem()
        {
            _proxyChannel = new TcpProxyChannel();
            _proxyChannel.DataFilter = this;
            _proxyChannel.ClientCountChanged += _proxyChannel_ClientCountChanged;
        }
        #endregion

        #region Properties
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }

        private int? _listenPort;
        public int? ListenPort
        {
            get { return _listenPort; }
            set
            {
                if (value != _listenPort)
                {
                    _listenPort = value;
                    NotifyPropertyChanged(nameof(ListenPort));
                }
            }
        }

        private string _remoteIP;
        public string RemoteIP
        {
            get { return _remoteIP; }
            set
            {
                if (value != _remoteIP)
                {
                    _remoteIP = value;
                    NotifyPropertyChanged(nameof(RemoteIP));
                }
            }
        }

        private int? _remotePort;
        public int? RemotePort
        {
            get { return _remotePort; }
            set
            {
                if (value != _remotePort)
                {
                    _remotePort = value;
                    NotifyPropertyChanged(nameof(RemotePort));
                }
            }
        }

        private bool _isRunning = false;
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if (value != _isRunning)
                {
                    _isRunning = value;
                    NotifyPropertyChanged(nameof(IsRunning));
                }
            }
        }


        private int _clientCount = 0;
        public int ClientCount
        {
            get { return _clientCount; }
            set
            {
                if (value != _clientCount)
                {
                    _clientCount = value;
                    NotifyPropertyChanged(nameof(ClientCount));
                }
            }
        }
        #endregion

        #region Model Functions

        public static ProxyChannelItem FromModel(ProxyChannel model)
        {
            ProxyChannelItem tempVM = new ProxyChannelItem();
            tempVM.Name = model.Name;
            tempVM.ListenPort = model.ListenPort;
            tempVM.RemoteIP = model.RemoteIP;
            tempVM.RemotePort = model.RemotePort;
            return tempVM;
        }

        public ProxyChannel ToModel()
        {
            ProxyChannel model = new ProxyChannel();
            model.Name = this.Name;
            model.ListenPort = this.ListenPort;
            model.RemoteIP = this.RemoteIP;
            model.RemotePort = this.RemotePort;
            return model;
        }

        public ProxyChannelItem Clone()
        {
            ProxyChannelItem pi = new ProxyChannelItem();
            pi.Import(this);
            return pi;
        }

        public void Import(ProxyChannelItem pi)
        {
            this.ListenPort = pi.ListenPort;
            this.Name = pi.Name;
            this.RemoteIP = pi.RemoteIP;
            this.RemotePort = pi.RemotePort;
        }

        #endregion

        #region Private members
        private TcpProxyChannel _proxyChannel;
        #endregion

        #region Private functions
        private void _proxyChannel_ClientCountChanged(object sender, ClientCountChangedEventArgs args)
        {
            ClientCount = args.NewCount;
        }
        #endregion

        #region Public functions
        public void StartService()
        {
            if (!_proxyChannel.IsRunning)
            {
                _proxyChannel.ListenPort = this.ListenPort.Value;
                _proxyChannel.RemoteIP = this.RemoteIP;
                _proxyChannel.RemotePort = this.RemotePort.Value;
                _proxyChannel.Start();
                IsRunning = _proxyChannel.IsRunning;
            }
        }

        public void StopService()
        {
            _proxyChannel.Stop();
            IsRunning = _proxyChannel.IsRunning;
            ClientCount = 0;
        }

        #endregion

        #region Implements for ITcpProxyDataFilter 

        public void BeforeClientToServer(TcpRawMessage clientMessage)
        {
            
        }

        public void BeforeServerToClient(TcpRawMessage serverMessage)
        {

        }

        #endregion
    }
}
