using System.Collections.Generic;

namespace FolderSyncer.Core.Monitor
{
    public interface IChannel
    {
        void AddFile(FileModel fileModel);
        IEnumerable<FileModel> GetFile();
    }
}
