using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using TcpProxy.Model;
using TcpProxy.ViewModel;

namespace TcpProxy
{

    public partial class MainWindow : Window
    {
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "channel.json");
        }
        #endregion

        #region Private members
        private string _configFilePath;
        private ObservableCollection<ProxyChannelItem> _channelVMList;
        #endregion

        #region Initialize
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            dgChannel.ItemsSource = _channelVMList;
        }
        #endregion

        #region Private functions

        private void LoadConfig()
        {
            _channelVMList = new ObservableCollection<ProxyChannelItem>();
            if (File.Exists(_configFilePath))
            {
                string strJson = File.ReadAllText(_configFilePath, Encoding.UTF8);
                List<ProxyChannel> channelList = JsonConvert.DeserializeObject<List<ProxyChannel>>(strJson);
                foreach (ProxyChannel channel in channelList)
                {
                    ProxyChannelItem pi = ProxyChannelItem.FromModel(channel);
                    _channelVMList.Add(pi);
                }
            }
        }

        private void SaveConfig()
        {
            List<ProxyChannel> channelList = new List<ProxyChannel>();
            foreach (ProxyChannelItem pi in _channelVMList)
            {
                channelList.Add(pi.ToModel());
            }
            string strJson = JsonConvert.SerializeObject(channelList);
            File.WriteAllText(_configFilePath, strJson);
        }
        private void LocalMsgBox(string strInfo)
        {
            MessageBox.Show(this, strInfo, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private MessageBoxResult QuestionOKCancel(string strInfo)
        {
            return MessageBox.Show(this, strInfo, this.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
        }
        #endregion

        #region UI Event Handlers
        private void btnStartService_Click(object sender, RoutedEventArgs e)
        {
            ProxyChannelItem pi = (sender as FrameworkElement).DataContext as ProxyChannelItem;
            try
            {
                pi.StartService();
            }
            catch (Exception ex)
            {
                LocalMsgBox(ex.Message);
            }
        }

        private void btnStopService_Click(object sender, RoutedEventArgs e)
        {
            ProxyChannelItem pi = (sender as FrameworkElement).DataContext as ProxyChannelItem;
            try
            {
                pi.StopService();
            }
            catch (Exception ex)
            {
                LocalMsgBox(ex.Message);
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            ProxyChannelItem pi = new ProxyChannelItem();
            AddOrEditChannelWnd addOrEditWnd = new AddOrEditChannelWnd(pi, false);
            if (addOrEditWnd.ShowDialog().Value)
            {
                _channelVMList.Add(pi);
                SaveConfig();
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            ProxyChannelItem selItem = dgChannel.SelectedItem as ProxyChannelItem;
            if (selItem == null)
            {
                LocalMsgBox("Please select the channel that you want to edit.");
                return;
            }
            if (selItem.IsRunning)
            {
                LocalMsgBox("Can not edit channel during running time.");
                return;
            }
            AddOrEditChannelWnd addOrEditWnd = new AddOrEditChannelWnd(selItem, false);
            if (addOrEditWnd.ShowDialog().Value)
            {
                SaveConfig();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            ProxyChannelItem selItem = dgChannel.SelectedItem as ProxyChannelItem;
            if (selItem == null)
            {
                LocalMsgBox("Please select the channel that you want to delete.");
                return;
            }
            if (selItem.IsRunning)
            {
                LocalMsgBox("Can not delete channel during running time.");
                return;
            }
            if (QuestionOKCancel("Are you sure to delete the channel?") == MessageBoxResult.Cancel)
            {
                return;
            }
            _channelVMList.Remove(selItem);
            SaveConfig();
        }
        #endregion


    }
}
