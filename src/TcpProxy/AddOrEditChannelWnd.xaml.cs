using System.Net;
using System.Windows;
using TcpProxy.ViewModel;

namespace TcpProxy
{
    public partial class AddOrEditChannelWnd : Window
    {
        public AddOrEditChannelWnd(ProxyChannelItem pi, bool isEdit)
        {
            InitializeComponent();
            IsEdit = isEdit;
            _uiContext = pi.Clone();
            _orgItem = pi;
            this.DataContext = _uiContext;
            this.Title = isEdit ? "Edit Channel" : "New Channel";
        }

        private ProxyChannelItem _uiContext;
        public ProxyChannelItem _orgItem { get; private set; }

        public bool IsEdit { get; private set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void LocalMsgBox(string strInfo)
        {
            MessageBox.Show(this, strInfo, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_uiContext.Name))
            {
                LocalMsgBox("Name is required");
                return;
            }

            if (_uiContext.ListenPort == null)
            {
                LocalMsgBox("Listen port is required");
                return;
            }
            if (_uiContext.ListenPort < 1 || _uiContext.ListenPort > 65535)
            {
                LocalMsgBox("Listen port must between 1 and 65535");
                return;
            }
            if (string.IsNullOrWhiteSpace(_uiContext.RemoteIP))
            {
                LocalMsgBox("Remote ip address is required");
                return;
            }

            if (!IPAddress.TryParse(_uiContext.RemoteIP, out IPAddress temp))
            {
                LocalMsgBox("Remote ip address is invalid");
                return;
            }

            if (_uiContext.RemotePort == null)
            {
                LocalMsgBox("Remote port is required");
                return;
            }
            if (_uiContext.RemotePort < 1 || _uiContext.RemotePort > 65535)
            {
                LocalMsgBox("Listen port must between 1 and 65535");
                return;
            }
            _orgItem.Import(_uiContext);
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
