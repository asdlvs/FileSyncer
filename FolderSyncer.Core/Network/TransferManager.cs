using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.TransferStrategies;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FolderSyncer.Core.Network
{
    /// <summary>
    /// Dispatcher class that reads events from channel,
    /// creates task for each event, chooses strategy and execute it.
    /// </summary>
    public class TransferManager : ITransferManager
    {
        private readonly IChannel _channel;
        private readonly IConfiguration _configuration;
        private readonly IFileSystem _fileSystem;
        private readonly Func<ISocket> _socketFactory;

        private readonly Dictionary<FileAction, Func<ITransferStrategy>> _transferStrategies;
        private readonly SemaphoreSlim _semaphore;
        private readonly object _genericLock;
        private readonly Dictionary<string, object> _locks;

        public TransferManager(IChannel channel, 
            IConfiguration configuration, 
            IFileSystem fileSystem,
            Func<ISocket> socketFactory,
            Dictionary<FileAction, Func<ITransferStrategy>> transferStrategies)
        {
            if (channel == null) { throw new ArgumentNullException(nameof(channel)); }
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }
            if (fileSystem == null) { throw new ArgumentNullException(nameof(fileSystem)); }
            if (socketFactory == null) { throw new ArgumentNullException(nameof(socketFactory)); }
            if (transferStrategies == null) { throw new ArgumentNullException(nameof(transferStrategies)); }

            _channel = channel;
            _configuration = configuration;
            _socketFactory = socketFactory;
            _fileSystem = fileSystem;
            _transferStrategies = transferStrategies;
            _genericLock = new object();
            _locks = new Dictionary<string, object>();

            if (!int.TryParse(_configuration["degree-of-parallelism"], out var degreeOfParallelism))
            {
                degreeOfParallelism = 10;
            }

            _semaphore = new SemaphoreSlim(degreeOfParallelism);
        }

        public TransferManager(IChannel channel,
            IConfiguration configuration,
            IFileSystem fileSystem,
            Func<ISocket> socketFactory,
            Func<HashAlgorithm> hashAlgorithm) : this(channel, configuration, fileSystem, socketFactory,
            new Dictionary<FileAction, Func<ITransferStrategy>>
            {
                {FileAction.Change, () => new ChangeTransferStrategy(configuration, fileSystem, hashAlgorithm())},
                {FileAction.Create, () => new ChangeTransferStrategy(configuration, fileSystem, hashAlgorithm())},
                {FileAction.Delete, () => new DeleteTransferStrategy()},
                {FileAction.Rename, () => new RenameTransferStrategy()}
            })
        { }

        public IEnumerable<Task> TransferData()
        {
            foreach (var fileModel in _channel.GetFile())
            {
                _semaphore.Wait();
                yield return Task.Run(() =>
                {
                    try
                    {
                        TransferFile(fileModel);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                        throw;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            }
        }

        public void TransferFile(FileModel fileModel)
        {
            lock (_genericLock)
            {
                if (!_locks.ContainsKey(fileModel.RelativePath))
                {
                    _locks[fileModel.RelativePath] = new object();
                }
            }

            lock (_locks[fileModel.RelativePath])
            {
                if (fileModel.FileAction == FileAction.Change)
                {
                    WaitWhileFileIsLocked(fileModel);
                }

                fileModel.FileType = GetFileType(fileModel.FullPath);
                Log.Information($"Sending {fileModel.FileAction} event for file {fileModel.RelativePath}.");
                using (var socket = _socketFactory().Connect(_configuration["ip"], int.Parse(_configuration["port"])))
                using (var destinationStream = socket.GetStream())
                {
                    var currentStrategy = _transferStrategies[fileModel.FileAction]();
                    currentStrategy.Execute(fileModel, destinationStream);
                }
                Log.Information($"Sended {fileModel.FileAction} event for file {fileModel.RelativePath}.");
            }
        }

        private void WaitWhileFileIsLocked(FileModel fileModel)
        {
            while (IsFileLocked(fileModel.FullPath))
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// The only way to understand if file is locked by copying, moving and so on.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsFileLocked(string path)
        {
            Stream stream = null;

            try
            {
                stream = _fileSystem.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }

        private FileType GetFileType(string absolutePath)
        {
            if (!_fileSystem.File.Exists(absolutePath) &&
                !_fileSystem.Directory.Exists(absolutePath))
            {
                return FileType.Unknown;
            }

            FileAttributes attr = _fileSystem.File.GetAttributes(absolutePath);
            return attr.HasFlag(FileAttributes.Directory) ?
                FileType.Directory :
                FileType.Regular;
        }
    }
}