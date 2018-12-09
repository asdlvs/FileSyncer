namespace FolderSyncer.Core.Monitor
{
    public enum FileAction : byte
    {
        Create = 1,
        Change = 2,
        Delete = 3,
        Rename = 4
    }
}