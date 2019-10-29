using SiS.Communication.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpProxy.Model;

namespace TcpProxy.ViewModel
{
    public class ProxyChannelItem : NotifyBase
    {
        public ProxyChannelItem()
        {
            _proxyChannel = new TcpProxyChannel();
        }

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
        private TcpProxyChannel _proxyChannel;
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
        }
    }
}
