using System.IO;
using FolderSyncer.Core.Monitor;

namespace FolderSyncer.Core.Network.TransferStrategies
{
    public interface ITransferStrategy
    {
        void Execute(FileModel fileModel, Stream stream);
    }
}
