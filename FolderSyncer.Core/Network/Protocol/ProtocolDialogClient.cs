using System;
using System.IO;
using System.Text;

namespace FolderSyncer.Core.Network.Protocol
{
    /// <summary>
    /// Helper to generate client protocol messages.
    /// </summary>
    public class ProtocolDialogClient : IProtocolDialogClient
    {
        private readonly Stream _destination;

        public ProtocolDialogClient(Stream destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }

            _destination = destination;
        }

        public bool ActionIsNeeded()
        {
            byte[] needToSend = new byte[1];
            _destination.Read(needToSend, 0, needToSend.Length);
            return needToSend[0] == 1;
        }

        public void InitiateDialog(ProtocolDialogData data)
        {
            SendBytes(new[] { data.Version });
            SendBytes(new[] { (byte)data.FileAction });
            SendBytes(new[] { (byte)data.FileType });
            SendBytes(BitConverter.GetBytes(data.RelativeFilePath.Length));
            SendBytes(Encoding.GetEncoding("UTF-8").GetBytes(data.RelativeFilePath));
        }

        public void SendBytes(byte[] data, int? size = null)
        {
            _destination.Write(data, 0, size ?? data.Length);
        }
    }
}