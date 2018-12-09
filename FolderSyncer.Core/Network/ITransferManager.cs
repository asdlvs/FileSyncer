using System.Collections.Generic;
using System.Threading.Tasks;
using FolderSyncer.Core.Monitor;

namespace FolderSyncer.Core.Network
{
    public interface ITransferManager
    {
        IEnumerable<Task> TransferData();
        void TransferFile(FileModel fileModel);
    }
}