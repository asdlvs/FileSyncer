using System;
using System.IO;
using System.Linq;
using System.Text;
using FolderSyncer.Core.Monitor;
using FolderSyncer.Core.Network.TransferStrategies;
using Xunit;

namespace FolderSyncer.Core.Tests.Network.TransferStrategies
{
    public class DeleteTransferStrategyTests
    {
        [Fact]
        public void Execute_WithNullModel_ShouldShrowException()
        {
            // Arrange
            // Act
            // Assert
            var strategy = new DeleteTransferStrategy();
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(null, new MemoryStream()));
        }

        [Fact]
        public void Execute_WithNullStream_ShouldShrowException()
        {
            // Arrange
            // Act
            // Assert
            var strategy = new DeleteTransferStrategy();
            Assert.Throws<ArgumentNullException>(() => strategy.Execute(new RenameFileModel(), null));
        }

        [Fact]
        public void Execute_ShouldInitiateDialogWithCorrectValues()
        {
            // Arrange
            // Act
            var strategy = new DeleteTransferStrategy();
            var memoryStream = new MemoryStream();
            strategy.Execute(new FileModel
            {
                FileAction = FileAction.Delete,
                FullPath = "C:\\MyFolder\\1.txt",
                RelativePath = "1.txt",
                FileType = FileType.Regular
            }, memoryStream);

            // Assert
            memoryStream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(memoryStream);

            var version = reader.ReadBytes(1).Single();
            Assert.Equal(1, version);

            var action = (FileAction)reader.ReadBytes(1).Single();
            Assert.Equal(FileAction.Delete, action);

            var type = (FileType)reader.ReadBytes(1).Single();
            Assert.Equal(FileType.Regular, type);

            var size = BitConverter.ToInt32(reader.ReadBytes(4), 0);
            Assert.Equal(5, size);

            var filename = Encoding.GetEncoding("UTF-8").GetString(reader.ReadBytes(size));
            Assert.Equal("1.txt", filename);
        }
    }
}
