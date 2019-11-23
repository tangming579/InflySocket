using InflySocket;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace ExSimpleClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        InflyClientSimple client = new InflyClientSimple();

        public MainWindow()
        {
            InitializeComponent();

            client.OnReceviceMessageEvent += Client_OnReceviceMessageEvent;
            client.OnConnectedEvent += Client_OnConnectedEvent;
            client.OnCloseEvent += Client_OnCloseEvent;
        }

        private void Client_OnReceviceMessageEvent(string msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txbMsg.AppendText($"收到消息：{msg}{'\n'}");
            }));
        }

        private void Client_OnConnectedEvent()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txbMsg.AppendText($"连接{'\n'}");
            }));
        }

        private void Client_OnCloseEvent()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txbMsg.AppendText($"已断开{'\n'}");
            }));
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            client.Connect(txbIP.Text, int.Parse(txbPort.Text));
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            client.Send(txbSend.Text);
        }
    }
}
