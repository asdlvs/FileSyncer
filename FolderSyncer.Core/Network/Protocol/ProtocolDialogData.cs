using FolderSyncer.Core.Monitor;

namespace FolderSyncer.Core.Network.Protocol
{
    public class ProtocolDialogData
    {
        public byte Version { get; set; }
        public FileAction FileAction { get; set; }
        public string RelativeFilePath { get; set; }
        public FileType FileType { get; set; }
    }
}
