using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.Protocol;
using Microsoft.Extensions.Configuration;

namespace FolderSyncer.Core.Network.ReceiverStrategies
{
    /// <summary>
    /// Check if event was received for file or folder.
    /// If it was received for file - it execute ChangeReceverStrategy,
    /// if for folder - just create it.
    /// </summary>
    public class CreateReceiverStrategy : ChangeFileReceiverStrategy
    {
        private readonly IFileSystem _fileSystem;
        private readonly IConfiguration _configuration;
        private readonly IProtocolDialogServer _dialog;

        public CreateReceiverStrategy(IProtocolDialogServer dialog, IConfiguration configuration, HashAlgorithm hashAlgorithm, IFileSystem fileSystem) : 
            base(dialog, configuration, hashAlgorithm, fileSystem)
        {
            if (dialog == null) { throw new ArgumentNullException(nameof(dialog)); }
            if (fileSystem == null) { throw new ArgumentNullException(nameof(fileSystem)); }
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }

            _dialog = dialog;
            _fileSystem = fileSystem;
            _configuration = configuration;
        }

        public override void ProcessRequest()
        {
            string absolutePath = _fileSystem.Path.Combine(_configuration["output-directory"], _dialog.DialogData.RelativeFilePath);
            if (_dialog.DialogData.FileType == FileType.Directory)
            {
                _fileSystem.Directory.CreateDirectory(absolutePath);
            }
            else
            {
                base.ProcessRequest();
            }
            
        }
    }
}
