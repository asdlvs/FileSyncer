using System.IO;
using System.Net;
using System.Net.Sockets;

namespace FolderSyncer.Core.Network
{
    /// <summary>
    /// Testabe wrapper on System.Net.Sockets.Socket
    /// </summary>
    public class Socket : ISocket
    {
        private readonly System.Net.Sockets.Socket _socket;

        public Socket(System.Net.Sockets.Socket socket)
        {
            _socket = socket;
        }

        public void Dispose()
        {
            _socket.Shutdown(SocketShutdown.Send);
            _socket.Dispose();
        }

        public Stream GetStream()
        {
            return new NetworkStream(_socket);
        }

        public ISocket Accept()
        {
            var socket = _socket.Accept();
            return new Socket(socket);
        }

        public ISocket Connect(string ip, int port)
        {
            _socket.Connect(ip, port);
            return this;
        }

        public ISocket Bind(string ip, int port)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            return this;
        }

        public ISocket Listen(int connections)
        {
            _socket.Listen(32);
            return this;
        }
    }
}
