using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using FolderSyncer.Core.Network.Protocol;
using Microsoft.Extensions.Configuration;

namespace FolderSyncer.Core.Network.ReceiverStrategies
{
    /// <summary>
    /// Rename file or folder.
    /// </summary>
    public class RenameReceiverStrategy : IFileReceiverStrategy
    {
        private readonly IFileSystem _fileSystem;
        private readonly IConfiguration _configuration;
        private readonly IProtocolDialogServer _dialog;

        public RenameReceiverStrategy(IProtocolDialogServer dialog, IConfiguration configuration, IFileSystem fileSystem)
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
            var (from, to) = GetPaths();
            string absolutePath = _fileSystem.Path.Combine(_configuration["output-directory"], _dialog.DialogData.RelativeFilePath);
            FileAttributes attr = _fileSystem.File.GetAttributes(absolutePath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                if (_fileSystem.Directory.Exists(to))
                {
                    _fileSystem.Directory.Delete(to);
                }
                _fileSystem.Directory.Move(from, to);
            }
            else
            {
                _fileSystem.File.Move(from, to);
            }
        }

        private (string, string) GetPaths()
        {
            string outputDirectory = _configuration["output-directory"];
            int newFilePathSize = _dialog.ReadValue(sizeof(int), value => BitConverter.ToInt32(value, 0));
            string newRelativeFilePath = _dialog.ReadValue(newFilePathSize, value => Encoding.GetEncoding("UTF-8").GetString(value));
            return (
                _fileSystem.Path.Combine(outputDirectory, _dialog.DialogData.RelativeFilePath),
                _fileSystem.Path.Combine(outputDirectory, newRelativeFilePath));
        }
    }
}
