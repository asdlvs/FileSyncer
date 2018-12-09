namespace FolderSyncer.Core.Network.Protocol
{
    public interface IProtocolDialogClient
    {
        void InitiateDialog(ProtocolDialogData data);

        void SendBytes(byte[] bytes, int? size = null);

        bool ActionIsNeeded();
    }
}
