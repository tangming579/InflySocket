using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace InflySocket
{
    public class InflyServerSimple
    {
        #region Public
        public char separator = '#';
        public delegate void OnNewConnectedHandler(SessionBase newClient);
        public delegate void OnReceviceMessageHandler(string msg);

        public event OnNewConnectedHandler OnNewConnectedEvent;
        public event OnReceviceMessageHandler OnReceviceMessageEvent;
        public event OnNewConnectedHandler OnCloseEvent;

        #endregion

        #region Socket
        private bool running;
        private Socket socket;

        public List<SessionBase> Clients = new List<SessionBase>();//tcp客户端字典

        public bool Listen(int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            try
            {
                // 将负责监听的套接字绑定到唯一的ip和端口上；
                socket.Bind(endPoint);
            }
            catch
            {
                return false;
            }
            // 设置监听队列的长度；
            socket.Listen(100);
            running = true;
            Task.Run(() =>
            {
                ListenConnectingAsync();
            });
            return true;
        }
        /// <summary>
        /// 监听客户端请求的方法；
        /// </summary>
        private void ListenConnectingAsync()
        {
            while (running)  // 持续不断的监听客户端的连接请求；
            {
                try
                {
                    Socket sokConnection = socket.Accept(); // 一旦监听到一个客户端的请求，就返回一个与该客户端通信的 套接字；
                    // 将与客户端连接的 套接字 对象添加到集合中；
                    string str_EndPoint = sokConnection.RemoteEndPoint.ToString();
                    SessionBase myTcpClient = new SessionBase() { TcpSocket = sokConnection, EndPoint = str_EndPoint };
                    Clients.Add(myTcpClient);
                    OnNewConnected(myTcpClient);

                    Task.Run(() =>
                    {
                        ProcessLinesAsync(myTcpClient).ConfigureAwait(false);
                    });                    
                }
                catch (Exception exp)
                {

                }
                Thread.Sleep(200);
            }
        }

        public void Close()
        {
            running = false;
            socket.Close();
        }

        public void Send(byte[] buf)
        {
            foreach (var client in Clients)
            {
                client.Send(buf);
            }
        }

        public void Send(string msg)
        {
            var buf = System.Text.Encoding.UTF8.GetBytes(msg);
            foreach (var client in Clients)
            {
                client.Send(buf);
            }
        }

        protected virtual void OnNewConnected(SessionBase newClient)
        {
            Console.WriteLine(newClient.EndPoint);
            OnNewConnectedEvent?.Invoke(newClient);
        }

        protected virtual void OnReceviceMessage(string msg)
        {
            Console.WriteLine(msg);
            OnReceviceMessageEvent?.Invoke(msg);
        }

        protected virtual void OnClientClose(SessionBase newClient)
        {
            OnCloseEvent?.Invoke(newClient);
        }

        #endregion

        #region Pipelines

        async Task ProcessLinesAsync(SessionBase sessionBase)
        {
            var pipe = new Pipe();
            Task writing = FillPipeAsync(sessionBase.TcpSocket, pipe.Writer);
            Task reading = ReadPipeAsync(pipe.Reader);

            await Task.WhenAll(reading, writing);

            OnClientClose(sessionBase);
        }

        //写入循环
        async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {
            const int minimumBufferSize = 512;

            while (running)
            {
                //从PipeWriter至少分配512字节
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                try
                {
                    //将内存空间变成ArraySegment，提供给socket使用
                    if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out ArraySegment<byte> arraySegment))
                    {
                        throw new InvalidOperationException("Buffer backed by array was expected");
                    }
                    //接受数据
                    int bytesRead = await SocketTaskExtensions.ReceiveAsync(socket, arraySegment, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    //一次接受完毕，数据已经在pipe中，告诉pipe已经给它写了多少数据。
                    writer.Advance(bytesRead);
                }
                catch
                {
                    break;
                }
                // 提示reader可以进行读取数据，reader可以继续执行readAsync()方法
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }
            // 告诉pipe已完成
            writer.Complete();
        }

        //读取流
        async Task ReadPipeAsync(PipeReader reader)
        {
            while (running)
            {
                //等待writer写数据
                ReadResult result = await reader.ReadAsync();
                //获得内存区域
                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition? position = null;

                do
                {
                    // 在缓冲数据中查找找一个行末尾
                    position = buffer.PositionOf((byte)separator);

                    if (position != null)
                    {
                        // 处理这一行
                        ProcessLine(buffer.Slice(0, position.Value).ToArray());

                        // 跳过 这一行
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);
                //数据处理完毕，告诉pipe还剩下多少数据没有处理（数据包不完整的数据，找不到head）
                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
            //将PipeReader标记为完成
            reader.Complete();
        }

        private void ProcessLine(byte[] data)
        {
            string msg = System.Text.Encoding.UTF8.GetString(data);
            OnReceviceMessage(msg);
        }
        #endregion
    }
}
