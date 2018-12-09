using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.Protocol;
using Microsoft.Extensions.Configuration;

namespace FolderSyncer.Core.Network.TransferStrategies
{
    /// <summary>
    /// Send hash of file to the server and wait for answer (dialog.ActionIsNeeded()).
    /// If answer is true, starting uploading file segment by segment.
    /// For each segment it calcualte its hash, send this hash to server and wait for answer.
    /// If answer is try - upload segment, if fasle - skip it.
    /// </summary>
    public class ChangeTransferStrategy : ITransferStrategy
    {
        private readonly HashAlgorithm _hash;
        private readonly IConfiguration _configuration;
        private readonly IFileSystem _fileSystem;

        public ChangeTransferStrategy(IConfiguration configuration, IFileSystem fileSystem, HashAlgorithm hash)
        {
            if (hash == null) { throw new ArgumentNullException(nameof(hash)); }
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }
            if (fileSystem == null) { throw new ArgumentNullException(nameof(fileSystem)); }

            _hash = hash;
            _fileSystem = fileSystem;
            _configuration = configuration;
        }

        public virtual void Execute(FileModel fileModel, Stream stream)
        {
            if (fileModel == null) { throw new ArgumentNullException(nameof(fileModel)); }
            if (stream == null) { throw new ArgumentNullException(nameof(stream)); }

            var dialog = new ProtocolDialogClient(stream);
            dialog.InitiateDialog(new ProtocolDialogData
            {
                FileAction = fileModel.FileAction,
                Version = 1,
                RelativeFilePath = fileModel.RelativePath,
                FileType = fileModel.FileType 
            });

            if (fileModel.FileType == FileType.Directory) { return; }

            using (var sourceStream = _fileSystem.File.OpenRead(fileModel.FullPath))
            {
                var fileHash = _hash.ComputeHash(sourceStream);
                dialog.SendBytes(fileHash);
                sourceStream.Seek(0, SeekOrigin.Begin);

                if (dialog.ActionIsNeeded())
                {
                    int readedBytes;
                    byte[] buffer = new byte[int.Parse(_configuration["segment-size"])];
                    while ((readedBytes = sourceStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        SendSegment(dialog, buffer, readedBytes);
                    }
                }
            }
        }

        private void SendSegment(IProtocolDialogClient dialog, byte[] segment, int size)
        {
            var segmentHash = _hash.ComputeHash(segment);
            dialog.SendBytes(segmentHash);

            if (dialog.ActionIsNeeded())
            {
                dialog.SendBytes(segment, size);
            }
        }
    }
}
