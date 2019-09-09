using System;
using System.Windows;
using System.Windows.Controls;

namespace SiS.Communication.Demo
{
    public partial class PageBasicView : UserControl
    {
        public PageBasicView()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _pageBaseVM = this.DataContext as PageBaseViewModel;
        }

        private PageBaseViewModel _pageBaseVM;

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _pageBaseVM.ServerVM.StartServer();
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
                _pageBaseVM.ServerVM.StopServer();
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
                _pageBaseVM.ClientVM.ConnectToServer();
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
                _pageBaseVM.ClientVM.Disconnect();
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
                _pageBaseVM.ServerVM.ServerSend();
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
                _pageBaseVM.ClientVM.ClientSend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox).ScrollToEnd();
        }
    }
}
