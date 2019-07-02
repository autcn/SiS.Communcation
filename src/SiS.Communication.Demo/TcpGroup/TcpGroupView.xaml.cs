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
    public partial class TcpGroupView : UserControl
    {
        public TcpGroupView()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _tcpGroupVM = this.DataContext as TcpGroupViewModel;
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox).ScrollToEnd();
        }

        private TcpGroupViewModel _tcpGroupVM;

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _tcpGroupVM.ServerVM.StartServer();
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
                _tcpGroupVM.ServerVM.StopServer();
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
                TcpGroupClientViewModel clientVM = (sender as FrameworkElement).DataContext as TcpGroupClientViewModel;
                clientVM.ConnectToServer();
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
                TcpGroupClientViewModel clientVM = (sender as FrameworkElement).DataContext as TcpGroupClientViewModel;
                clientVM.Disconnect();
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
                _tcpGroupVM.ServerVM.ServerSend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnClientSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpGroupClientViewModel clientVM = (sender as FrameworkElement).DataContext as TcpGroupClientViewModel;
                clientVM.ClientSend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnServerSendGroupMsg_Click(object sender, RoutedEventArgs e)
        {
            (_tcpGroupVM.ServerVM as TcpGroupServerViewModel).SendGroupMessage();
        }

        private void BtnClientSendGroupMsg_Click(object sender, RoutedEventArgs e)
        {
            TcpGroupClientViewModel clientVM = (sender as FrameworkElement).DataContext as TcpGroupClientViewModel;
            clientVM.SendGroupMessage();
        }



        private void btnCloseClient_Click(object sender, RoutedEventArgs e)
        {
            (_tcpGroupVM.ServerVM as TcpGroupServerViewModel).CloseClient();
        }
    }
}
