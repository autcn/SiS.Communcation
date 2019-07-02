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
