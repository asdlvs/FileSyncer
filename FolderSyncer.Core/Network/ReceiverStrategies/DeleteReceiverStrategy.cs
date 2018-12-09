using System;
using System.IO;
using System.IO.Abstractions;
using FolderSyncer.Core.Network.Protocol;
using Microsoft.Extensions.Configuration;

namespace FolderSyncer.Core.Network.ReceiverStrategies
{
    /// <summary>
    /// Delete file of folder
    /// </summary>
    public class DeleteReceiverStrategy : IFileReceiverStrategy
    {
        private readonly IFileSystem _fileSystem;
        private readonly IConfiguration _configuration;
        private readonly IProtocolDialogServer _dialog;

        public DeleteReceiverStrategy(IProtocolDialogServer dialog, IConfiguration configuration, IFileSystem fileSystem)
        {
            if (dialog == null) { throw new ArgumentNullException(nameof(dialog)); }
            if (fileSystem == null) { throw new ArgumentNullException(nameof(fileSystem)); }
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }

            _dialog = dialog;
            _fileSystem = fileSystem;
            _configuration = configuration;
        }

        public void ProcessRequest()
        {
            string absolutePath = _fileSystem.Path.Combine(_configuration["output-directory"], _dialog.DialogData.RelativeFilePath);
            FileAttributes attr = _fileSystem.File.GetAttributes(absolutePath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                _fileSystem.Directory.Delete(absolutePath);
            }
            else
            {
                _fileSystem.File.Delete(absolutePath);
            }
        }
    }
}
