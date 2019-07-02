using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SiS.Communication.Demo
{
    public partial class UdpView : UserControl
    {
        public UdpView()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _udpVM = this.DataContext as UdpViewModel;
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox).ScrollToEnd();
        }

        private UdpViewModel _udpVM;

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _udpVM.ServerVM.StartServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStopServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _udpVM.ServerVM.StopServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _udpVM.ClientVM.ConnectToServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _udpVM.ClientVM.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnServerSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _udpVM.ServerVM.ServerSend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void BtnConnectToServer_Click(object sender, MouseButtonEventArgs e)
        {
            _udpVM.ClientVM.ConnectToServer();
        }

        private void BtnClientSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _udpVM.ClientVM.ClientSend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnServerSendGroupMsg_Click(object sender, RoutedEventArgs e)
        {
            (_udpVM.ServerVM as UdpServerViewModel).SendGroupMessage();
        }

        private void BtnClientSendGroupMsg_Click(object sender, RoutedEventArgs e)
        {
            (_udpVM.ClientVM as UdpClientViewModel).SendGroupMessage();
        }

        
    }
}
