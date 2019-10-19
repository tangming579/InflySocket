using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InflySocket
{
    public class InflyClientSimple
    {
        private bool running;
        private Socket socket;

        public void Connect(string ip, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress iPAddress = IPAddress.Parse(ip);
            IPEndPoint point = new IPEndPoint(iPAddress, port);

            Task.Run(() =>
            {
                while (running)
                {
                    if (!socket.Connected)
                    {
                        socket.Connect(point);
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        Received();
                    }
                }

            });
        }

        protected virtual void Received()
        {
            while (running & socket.Connected)
            {

            }
        }

        protected virtual void Send(byte[] buf)
        {
            if (socket.Connected)
                socket.Send(buf);
        }

        protected virtual void Send(string msg)
        {
            var sendMsg = System.Text.Encoding.UTF8.GetBytes(msg);
            Send(sendMsg);
        }
    }
}
