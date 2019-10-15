using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace InflySocket
{
    public class SessionBase
    {
        public string EndPoint { get; set; }

        public Socket TcpSocket { get; internal set; }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="buf"></param>
        public void Send(byte[] buf)
        {
            if (buf != null)
            {
                TcpSocket.Send(buf);
            }
        }
        /// <summary>
        /// 获取连接的ip
        /// </summary>
        /// <returns></returns>
        public string GetIp()
        {
            IPEndPoint clientipe = (IPEndPoint)TcpSocket.RemoteEndPoint;
            string _ip = clientipe.Address.ToString();
            return _ip;
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            TcpSocket.Shutdown(SocketShutdown.Both);
        }
    }
}
