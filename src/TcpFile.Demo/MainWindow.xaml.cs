using System.Collections.Generic;
using System.Windows;

namespace TcpFile.Demo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private ServerWnd _serverWnd;

        private void btnOpenServer_Click(object sender, RoutedEventArgs e)
        {

            if (_serverWnd == null)
            {
                _serverWnd = new ServerWnd();
            }
            _serverWnd.Show();
            _serverWnd.WindowState = WindowState.Normal;
        }

        private List<ClientWnd> _clientWnds = new List<ClientWnd>();

        private void btnOpenClient_Click(object sender, RoutedEventArgs e)
        {
            ClientWnd _clientWnd = new ClientWnd();
            _clientWnds.Add(_clientWnd);
            _clientWnd.Show();
        }
    }
}
