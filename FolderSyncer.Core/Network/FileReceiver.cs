using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.Protocol;
using FolderSyncer.Core.Network.ReceiverStrategies;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FolderSyncer.Core.Network
{
    /// <summary>
    /// Dispatcher class that creates task for each received connection,
    /// choose strategy execute it.
    /// </summary>
    public class FileReceiver : IFileReceiver
    {
        private readonly IConfiguration _configuration;
        private readonly ISocket _socket;
        private readonly Dictionary<FileAction, Func<IProtocolDialogServer, IFileReceiverStrategy>> _fileStrategies;

        public FileReceiver(IConfiguration configuration, IFileSystem fileSystem, ISocket socket, Func<HashAlgorithm> hashAlgorithm) :
            this(configuration, 
                socket,
                new Dictionary<FileAction, Func<IProtocolDialogServer, IFileReceiverStrategy>>
                {
                    { FileAction.Change, dialog => new ChangeReceiverStrategy(dialog, configuration, hashAlgorithm(), fileSystem) },
                    { FileAction.Create, dialog => new CreateReceiverStrategy(dialog, configuration, hashAlgorithm(), fileSystem) },
                    { FileAction.Delete, dialog => new DeleteReceiverStrategy(dialog, configuration, fileSystem) },
                    { FileAction.Rename, dialog => new RenameReceiverStrategy(dialog, configuration, fileSystem) }
                })
        {
        }

        public FileReceiver(IConfiguration configuration, ISocket socket,
            Dictionary<FileAction, Func<IProtocolDialogServer, IFileReceiverStrategy>> fileStrategies)
        {
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }
            if (socket == null) { throw new ArgumentNullException(nameof(socket)); }
            if (fileStrategies == null) { throw new ArgumentNullException(nameof(fileStrategies)); }

            _configuration = configuration;
            _socket = socket;
            _fileStrategies = fileStrategies;
        }

        public IEnumerable<Task> StartServer()
        {
            var serverSocket = _socket
                .Bind(_configuration["ip"], int.Parse(_configuration["port"]))
                .Listen(32);

            while (true)
            {
                var acceptedConnection = serverSocket.Accept();
                yield return Task.Run(() => ProcessAcceptedConnection(acceptedConnection));
            }
        }

        private void ProcessAcceptedConnection(ISocket acceptedConnection)
        {
            try
            {
                using (var sourceStream = acceptedConnection.GetStream())
                {
                    var dialogServer = new ProtocolDialogServer(sourceStream);
                    var dialogData = dialogServer.AcceptDialog();

                    Log.Information(
                        $"Received event {dialogData.FileAction} for file {dialogData.RelativeFilePath}.");

                    var constructor = _fileStrategies[dialogData.FileAction];
                    IFileReceiverStrategy currentStrategy = constructor(dialogServer);
                    currentStrategy.ProcessRequest();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw;
            }
            finally
            {
                acceptedConnection.Dispose();
            }
        }
    }
}
