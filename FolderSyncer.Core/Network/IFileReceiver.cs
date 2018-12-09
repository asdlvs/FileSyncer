using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSyncer.Core.Network
{
    public interface IFileReceiver
    {
        IEnumerable<Task> StartServer();
    }
}
