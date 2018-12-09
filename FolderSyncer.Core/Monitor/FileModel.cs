namespace FolderSyncer.Core.Monitor
{
    public class FileModel
    {
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
        public FileAction FileAction { get; set; }
        public FileType FileType { get; set; }
    }
}
