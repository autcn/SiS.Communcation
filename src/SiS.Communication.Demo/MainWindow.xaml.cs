using System.Collections.Generic;
using System.Windows;

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
                new HttpViewModel()
            };
            foreach(PageBaseViewModel vm in _tabDataSource)
            {
                vm.Initialize();
            }
            tabCtrl.ItemsSource = _tabDataSource;
        }
    }
}
