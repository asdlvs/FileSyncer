using System;
using System.IO;

namespace FolderSyncer.Core.Network
{
    public interface ISocket : IDisposable
    {
        Stream GetStream();

        ISocket Connect(string ip, int port);

        ISocket Bind(string ip, int port);

        ISocket Listen(int connections);

        ISocket Accept();
    }
}
