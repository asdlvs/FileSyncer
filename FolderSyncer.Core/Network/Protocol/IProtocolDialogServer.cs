using System;

namespace FolderSyncer.Core.Network.Protocol
{
    public interface IProtocolDialogServer
    {
        ProtocolDialogData DialogData { get; }

        ProtocolDialogData AcceptDialog();

        bool HasData();

        void NotifyClient(bool actionIsNeeded);

        T ReadValue<T>(int size, Func<byte[], T> parseFunction);
    }
}