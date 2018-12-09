using System;
using System.IO;
using System.Linq;
using System.Text;
using FolderSyncer.Core.Monitor;

namespace FolderSyncer.Core.Network.Protocol
{
    /// <summary>
    /// Helper class to read protocol messages.
    /// </summary>
    public class ProtocolDialogServer : IProtocolDialogServer
    {
        private readonly Stream _inputStream;
        private ProtocolDialogData _dialogData;
        private bool _hasData;

        public ProtocolDialogServer(Stream inputStream)
        {
            if (inputStream == null) { throw new ArgumentNullException(nameof(inputStream)); }

            _inputStream = inputStream;
            _hasData = true;
        }

        public ProtocolDialogData DialogData => _dialogData;

        public ProtocolDialogData AcceptDialog()
        {
            var version = ReadValue(1, value => value[0]);
            var fileAction = ReadValue(1, value => (FileAction)value[0]);
            var fileType = ReadValue(1, value => (FileType)value[0]);
            var filePathSize = ReadValue(4, value => BitConverter.ToInt32(value, 0));
            var relativePath = ReadValue(filePathSize, value => Encoding.GetEncoding("UTF-8").GetString(value));

            _dialogData = new ProtocolDialogData
            {
                Version = version,
                FileAction = fileAction,
                RelativeFilePath = relativePath,
                FileType = fileType
            };

            return _dialogData;
        }

        public bool HasData()
        {
            return _hasData;
        }

        public void NotifyClient(bool actionIsNeeded)
        {
            byte[] value = { (byte)(actionIsNeeded ? 1 : 0) };
            _inputStream.Write(value, 0, value.Length);
        }

        public T ReadValue<T>(int size, Func<byte[], T> parseFunction)
        {
            using (var memory = new MemoryStream(size))
            {
                byte[] buffer = new byte[size];
                int summaryReceived = 0;
                int leftToRead = size;
                while (summaryReceived < size)
                {
                    int num = _inputStream.Read(buffer, 0, leftToRead);
                    if (num == 0) { break; }
                    summaryReceived += num;
                    leftToRead -= num;
                    memory.Write(buffer, 0, num);
                }

                _hasData = memory.Length == size;

                return parseFunction(memory.GetBuffer().Take(summaryReceived).ToArray());
            }
        }
    }
}