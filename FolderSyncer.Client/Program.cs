using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network;
using Microsoft.Extensions.Configuration;
using Serilog;
using Socket = FolderSyncer.Core.Network.Socket;

namespace FolderSyncer.Client
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

            IChannel channel = new Channel(new Timer(config));
            IFileSystem fileSystem  = new FileSystem();

            Task.Run(() => new DirectoryMonitor(config, channel, fileSystem).StartMonitoring());
            Task.Run(() =>
            {
                var transferManager = new TransferManager(channel, config, fileSystem, 
                    () => new Socket(
                        new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)),
                    () => new MD5CryptoServiceProvider());
                new List<Task>().AddRange(transferManager.TransferData());
            });
            
            Console.ReadKey();
        }
    }
}
