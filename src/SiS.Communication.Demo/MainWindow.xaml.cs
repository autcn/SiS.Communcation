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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private List<PageBaseViewModel> _tabDataSource;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _tabDataSource = new List<PageBaseViewModel>()
            {
                new TcpBasicViewModel(),
                new TcpGroupViewModel(),
                new TcpModelViewModel(),
                new PipeViewModel(),
                new ShareMemoryViewModel(),
                new ShareMemoryDuplexViewModel(),
                new UdpViewModel(),
            };
            tabCtrl.ItemsSource = _tabDataSource;
        }
    }
}
