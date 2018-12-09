using System;
using System.IO;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.Protocol;

namespace FolderSyncer.Core.Network.TransferStrategies
{
    /// <summary>
    /// Send delete event to server.
    /// </summary>
    public class DeleteTransferStrategy : ITransferStrategy
    {
        public void Execute(FileModel fileModel, Stream stream)
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
        }
    }
}
