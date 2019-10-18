using InflySocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PipeLinesDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        InflyServerSimple server = new InflyServerSimple();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            server.Close();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            server.OnNewConnectedEvent += Server_OnNewConnectedEvent;
            server.OnReceviceMessageEvent += Server_OnReceviceMessageEvent;
            server.OnCloseEvent += Server_OnCloseEvent;
        }

        private void Server_OnCloseEvent(SessionBase newClient)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txbMsg.AppendText($"已断开：{newClient.EndPoint}{'\n'}");
            }));
        }

        private void Server_OnReceviceMessageEvent(string msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txbMsg.AppendText($"收到消息：{msg}{'\n'}");
            }));            
        }

        private void Server_OnNewConnectedEvent(SessionBase newClient)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txbClients.AppendText($"新连接：{newClient.EndPoint}{'\n'}");
            }));            
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var isStart = server.Listen(9999);
            btnStart.Content = isStart ? "已启动" : "启动";
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            server.Send(txbSend.Text);
            txbMsg.AppendText($"发送消息：{txbSend.Text}{'\n'}");
        }
    }
}
