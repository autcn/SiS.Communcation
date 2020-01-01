using System.Windows;
using System.Windows.Controls;

namespace SiS.Communication.Demo
{
    public partial class HttpView : UserControl
    {
        public HttpView()
        {
            InitializeComponent();
        }
        private HttpViewModel _httpVM;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _httpVM = this.DataContext as HttpViewModel;
        }

        private void btnStopHttpServer_Click(object sender, RoutedEventArgs e)
        {
            _httpVM.StopServer();
        }

        private void btnStartHttpServer_Click(object sender, RoutedEventArgs e)
        {
            _httpVM.StartServer();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox).ScrollToEnd();
        }

        private void btnWSSend_Click(object sender, RoutedEventArgs e)
        {
            _httpVM.Send();
        }

        private void btnWSDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _httpVM.Disconnect();
        }
    }
}
