using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FolderSyncer.Core.Network;
using Microsoft.Extensions.Configuration;
using Serilog;
using Socket = FolderSyncer.Core.Network.Socket;

namespace FolderSyncer.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .Build();

            IFileSystem fileSystem = new FileSystem();
            var fileReceiver = new FileReceiver(config, fileSystem, 
                new Socket(new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)),
                () => new MD5CryptoServiceProvider());

            new List<Task>().AddRange(fileReceiver.StartServer());

            Log.Debug("Press any button to exit.");
            Console.ReadKey();
        }
    }
}
